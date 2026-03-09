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
/// </summary>
[DisallowMultipleComponent]
public sealed class DialogueRunner : MonoBehaviour
{
    [Header("Graph")]
    [SerializeField] private DialogueGraph graph;

    [Header("Navegación")]
    [SerializeField] private MonoBehaviour navigatorBehaviour; // IGraphNavigator

    [Header("Controles")]
    [SerializeField] private KeyCode advanceKey = KeyCode.N;
    [SerializeField] private KeyCode gameplayAdvanceKey = KeyCode.M;

    // Servicios internos
    private IGraphNavigator _navigator;
    private readonly Dictionary<Type, IModuleExecutor> _executors     = new();
    private readonly List<IModuleExecutor>             _executorList  = new(); // para iteraciones sin alloc

    // Executor de choices: único con lógica especial (evento + hotkeys)
    private ChoiceModuleExecutor _choiceExecutor;

    // Estado del runner
    private DialogueNodeData _current;
    private bool             _nodeReadyToAdvance;
    private IModuleExecutor  _activeBlockingExecutor;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        _navigator = navigatorBehaviour as IGraphNavigator;

        if (graph == null)      { Debug.LogError("[DialogueRunner] Falta DialogueGraph.");  enabled = false; return; }
        if (_navigator == null) { Debug.LogError("[DialogueRunner] Falta IGraphNavigator."); enabled = false; return; }

        // Descubrir y registrar todos los executors presentes en la jerarquía
        foreach (var executor in GetComponentsInChildren<IModuleExecutor>())
            RegisterExecutor(executor);

        // Obtener el executor de choices para su lógica especial
        if (_executors.TryGetValue(typeof(ChoiceModule), out var ce))
        {
            _choiceExecutor = ce as ChoiceModuleExecutor;
            if (_choiceExecutor != null)
                _choiceExecutor.OnChoiceSelected += OnChoiceSelected;
        }

        // Inicializar executors que lo necesiten (ej. PortraitController necesita el grafo)
        foreach (var executor in _executorList)
            executor.Initialize(graph);

        _navigator.Init(graph);
    }

    private void Start()
    {
        _current = _navigator.StartNode();
        _ = ShowNodeAsync(_current);
    }

    private void Update()
    {
        if (_current == null) return;

        // Hotkeys de choice (1-4)
        if (_choiceExecutor != null && _choiceExecutor.TryConsumeHotkey(out _))
            return; // choiceExecutor ya dispara OnChoiceSelected

        if (Input.GetKeyDown(advanceKey))
        {
            bool anyForwarded = false;

            // Intentar snap en todos los executors (cada uno decide internamente si está activo)
            foreach (var executor in _executorList)
                if (executor.TryFastForward()) anyForwarded = true;

            if (anyForwarded) return;

            if (_nodeReadyToAdvance)
                AdvanceToNext();
        }

        if (Input.GetKeyDown(gameplayAdvanceKey) && _nodeReadyToAdvance)
            AdvanceToNext();
    }

    private async Task ShowNodeAsync(DialogueNodeData node)
    {
        _nodeReadyToAdvance     = false;
        _activeBlockingExecutor = null;

        if (node == null) { EndDialogue(); return; }

        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var localCts = _cts; // captura local: protege contra reemplazos por nueva llamada
        var ctx = new ModuleExecutionContext(node.GUID, localCts.Token);

        // Notificar inicio de nodo a todos los executors
        foreach (var executor in _executorList)
            executor.OnNodeBegin();

        // Ejecutar módulos en orden
        var pendingParallel = new List<Task>();

        foreach (var module in node.modules)
        {
            if (module == null) continue;
            if (!_executors.TryGetValue(module.GetType(), out var executor))
            {
                Debug.LogWarning($"[DialogueRunner] Sin executor para módulo tipo '{module.GetType().Name}'. Saltando.");
                continue;
            }

            if (localCts.IsCancellationRequested) goto done;

            switch (module.RunMode)
            {
                case ModuleRunMode.FireAndForget:
                    _ = executor.ExecuteAsync(module, ctx);
                    break;

                case ModuleRunMode.Parallel:
                    // Arranca y trackea — el runner continúa de inmediato
                    pendingParallel.Add(executor.ExecuteAsync(module, ctx));
                    break;

                case ModuleRunMode.Blocking:
                    // Primero sincroniza todos los Parallel pendientes
                    if (pendingParallel.Count > 0)
                    {
                        try { await Task.WhenAll(pendingParallel); }
                        catch (OperationCanceledException) { goto done; }
                        pendingParallel.Clear();
                    }

                    if (localCts.IsCancellationRequested) goto done;

                    // Luego espera este módulo
                    _activeBlockingExecutor = executor;
                    try { await executor.ExecuteAsync(module, ctx); }
                    catch (OperationCanceledException) { goto done; }
                    finally { _activeBlockingExecutor = null; }
                    break;
            }
        }

        done:
        // Solo marcamos listo si este ShowNodeAsync sigue siendo el activo (no fue cancelado)
        if (!node.IsChoiceNode && !localCts.IsCancellationRequested)
            _nodeReadyToAdvance = true;
    }

    private void OnChoiceSelected(string portName)
    {
        _nodeReadyToAdvance = false;
        _cts?.Cancel();

        var next = _navigator.NextFrom(_current, portName);
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

    private void AdvanceToNext()
    {
        _nodeReadyToAdvance = false;
        _cts?.Cancel();

        var next = _navigator.NextFrom(_current);
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

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        if (_choiceExecutor != null)
            _choiceExecutor.OnChoiceSelected -= OnChoiceSelected;
    }

    private void RegisterExecutor(IModuleExecutor executor)
    {
        if (executor == null) return;
        _executors[executor.ModuleType] = executor;
        _executorList.Add(executor);
    }
}
