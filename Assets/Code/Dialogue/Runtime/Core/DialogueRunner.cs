using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Orquesta la ejecución de nodos de diálogo.
/// Cada nodo contiene una lista ordenada de módulos (<see cref="IDialogueModule"/>)
/// que se ejecutan secuencialmente a través de sus executors registrados.
/// El runner no conoce el contenido de los módulos: solo itera y despacha.
/// El input se delega en <see cref="DialogueInputController"/> (SRP).
/// Un módulo Blocking puede decidir su propia navegación devolviendo un puerto
/// desde <see cref="IModuleExecutor.ExecuteAsync"/> (ej. <see cref="ChoiceModule"/>);
/// si no, el runner avanza por el puerto "Next" cuando recibe input del jugador.
/// </summary>
[DisallowMultipleComponent]
public sealed class DialogueRunner : MonoBehaviour
{
    [Header("Graph")]
    [SerializeField] private DialogueGraph graph;

    [Header("Navegación")]
    [SerializeField] private MonoBehaviour navigatorBehaviour; // IGraphNavigator

    [Header("Controles")]
    [SerializeField] private DialogueInputController inputController;

    [Header("Pacing")]
    [Tooltip("Si esta activo, el runner espera input del jugador para avanzar tras un nodo sin decision de navegacion (Retratos). Si esta desactivado, avanza solo al puerto \"Next\" (Chat).")]
    [SerializeField] private bool waitForInputToAdvance = true;

    // Servicios internos
    private IGraphNavigator _navigator;
    private readonly Dictionary<Type, IModuleExecutor> _executors    = new();
    private readonly List<IModuleExecutor>             _executorList = new();

    // Estado del runner
    private DialogueNodeData        _current;
    private bool                    _nodeReadyToAdvance;
    private IModuleExecutor         _activeBlockingExecutor;
    private CancellationTokenSource _cts;
    private CharacterDefinition     _currentProfile;

    /// <summary>GUID del nodo actual, o null si no hay dialogo en curso. Sirve para
    /// guardar por donde iba una conversacion y retomarla luego con StartChat.</summary>
    public string CurrentNodeGuid => _current?.GUID;

    private void Awake()
    {
        _navigator = navigatorBehaviour as IGraphNavigator;

        if (_navigator == null) { enabled = false; return; }

        // Descubrir y registrar todos los executors presentes en la jerarquía
        foreach (var executor in GetComponentsInChildren<IModuleExecutor>())
            RegisterExecutor(executor);

        // Si hay grafo asignado desde el inspector, inicializar ya (mismo timing de siempre:
        // antes de que corra ningun Start() de la escena). Si no lo hay (arranque bajo demanda,
        // ej. StartChat llamado mas tarde por una app de telefono), se inicializa entonces.
        if (graph != null)
            InitializeGraph(graph);

        // Suscribirse al input controller
        if (inputController != null)
        {
            inputController.OnAdvance         += HandleAdvance;
            inputController.OnGameplayAdvance += HandleGameplayAdvance;
        }
    }

    private void Start()
    {
        if (graph == null) return;

        // El grafo ya se inicializo en Awake() si estaba asignado desde el inspector;
        // aqui solo arrancamos la reproduccion, sin repetir Initialize()/navigator.Init().
        _current = _navigator.StartNode();
        _ = ShowNodeAsync(_current);
    }

    // -----------------------------------------------------------------------
    // API pública: arrancar/parar un diálogo en cualquier momento
    // -----------------------------------------------------------------------

    /// <summary>
    /// Arranca (o reinicia) el diálogo con el grafo indicado, cancelando cualquier
    /// diálogo en curso primero. Además del arranque automático de <see cref="graph"/>
    /// en <see cref="Start"/>, permite arrancar bajo demanda (ej. al abrir una app de chat).
    /// Si se indica <paramref name="resumeNodeGuid"/> y existe en el grafo, arranca ahi
    /// en vez del nodo de inicio (retomar una conversacion por donde se dejo).
    /// </summary>
    public void StartChat(DialogueGraph newGraph, string resumeNodeGuid = null)
    {
        if (newGraph == null || _navigator == null) return;

        Stop();

        graph = newGraph;
        _currentProfile = null;

        InitializeGraph(graph);

        var resumeNode = string.IsNullOrEmpty(resumeNodeGuid) ? null : graph.FindNode(resumeNodeGuid);
        _current = resumeNode ?? _navigator.StartNode();
        _ = ShowNodeAsync(_current);
    }

    private void InitializeGraph(DialogueGraph g)
    {
        foreach (var executor in _executorList)
            executor.Initialize(g);

        _navigator.Init(g);
    }

    /// <summary>Detiene el diálogo en curso y limpia el estado de todos los executors.</summary>
    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        EndDialogue();
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        if (inputController != null)
        {
            inputController.OnAdvance         -= HandleAdvance;
            inputController.OnGameplayAdvance -= HandleGameplayAdvance;
        }
    }

    // -----------------------------------------------------------------------
    // Handlers de input (recibidos desde DialogueInputController)
    // -----------------------------------------------------------------------

    private void HandleAdvance()
    {
        if (_current == null) return;

        bool anyForwarded = false;
        foreach (var executor in _executorList)
            if (executor.TryFastForward()) anyForwarded = true;

        if (!anyForwarded && _nodeReadyToAdvance)
            Advance(null);
    }

    private void HandleGameplayAdvance()
    {
        if (_current == null) return;
        if (_nodeReadyToAdvance)
            Advance(null);
    }

    // -----------------------------------------------------------------------
    // Ejecución de nodos
    // -----------------------------------------------------------------------

    private async Task ShowNodeAsync(DialogueNodeData node)
    {
        _nodeReadyToAdvance     = false;
        _activeBlockingExecutor = null;

        if (node == null) { EndDialogue(); return; }

        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var localCts = _cts; // captura local: protege contra reemplazos por nueva llamada
        var ctx = new ModuleExecutionContext(node.GUID, localCts.Token) { CurrentProfile = _currentProfile };

        // Notificar inicio de nodo a todos los executors
        foreach (var executor in _executorList)
            executor.OnNodeBegin();

        // Ejecutar módulos en orden
        var pendingParallel = new List<Task>();
        string decidedPort = null;

        foreach (var module in node.modules)
        {
            if (module == null) continue;
            if (!_executors.TryGetValue(module.GetType(), out var executor))
                continue;

            if (localCts.IsCancellationRequested) goto done;

            switch (module.RunMode)
            {
                case ModuleRunMode.FireAndForget:
                    _ = executor.ExecuteAsync(module, ctx);
                    break;

                case ModuleRunMode.Parallel:
                    pendingParallel.Add(executor.ExecuteAsync(module, ctx));
                    break;

                case ModuleRunMode.Blocking:
                    if (pendingParallel.Count > 0)
                    {
                        try { await Task.WhenAll(pendingParallel); }
                        catch (OperationCanceledException) { goto done; }
                        pendingParallel.Clear();
                    }

                    if (localCts.IsCancellationRequested) goto done;

                    _activeBlockingExecutor = executor;
                    string port = null;
                    try { port = await executor.ExecuteAsync(module, ctx); }
                    catch (OperationCanceledException) { goto done; }
                    finally { _activeBlockingExecutor = null; }

                    if (port != null)
                    {
                        decidedPort = port;
                        goto done;
                    }
                    break;
            }
        }

        done:
        if (localCts.IsCancellationRequested) return;

        // Persiste el perfil actual (fijado por ProfileModule) para el siguiente nodo,
        // igual que hacia ChatRunner._currentProfile entre llamadas a ProcessNodeAsync.
        _currentProfile = ctx.CurrentProfile;

        if (decidedPort != null)
        {
            Advance(decidedPort);
        }
        else if (!node.IsChoiceNode)
        {
            if (waitForInputToAdvance)
                _nodeReadyToAdvance = true;
            else
                Advance(null);
        }
    }

    /// <summary>
    /// Navega al siguiente nodo por el puerto indicado (null = puerto "Next" por defecto).
    /// Único punto de avance: lo usan tanto el input del jugador como un módulo
    /// Blocking que decide su propia navegación (ej. ChoiceModule).
    /// </summary>
    private void Advance(string portName)
    {
        _nodeReadyToAdvance = false;
        _cts?.Cancel();

        var next = _navigator.NextFrom(_current, portName ?? "Next");
        if (next != null)
        {
            _current = next;
            _ = ShowNodeAsync(_current);
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        _nodeReadyToAdvance     = false;
        _activeBlockingExecutor = null;
        _current = null;

        foreach (var executor in _executorList)
            executor.ResetAll();
    }

    private void RegisterExecutor(IModuleExecutor executor)
    {
        if (executor == null) return;
        _executors[executor.ModuleType] = executor;
        _executorList.Add(executor);
    }
}
