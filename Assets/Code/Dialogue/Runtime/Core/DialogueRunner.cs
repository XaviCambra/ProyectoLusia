using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
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

    [Header("Executors de módulos")]
    [SerializeField] private TextModuleExecutor    textExecutor;
    [SerializeField] private PortraitModuleExecutor portraitExecutor;
    [SerializeField] private EventModuleExecutor   eventExecutor;
    [SerializeField] private AudioModuleExecutor   audioExecutor;
    [SerializeField] private ChoiceModuleExecutor  choiceExecutor;

    [Header("Controles")]
    [SerializeField] private KeyCode advanceKey = KeyCode.N;

    // Servicios internos
    private IGraphNavigator _navigator;
    private readonly Dictionary<Type, IModuleExecutor> _executors = new();

    // Estado del runner
    private DialogueNodeData _current;
    private bool             _nodeReadyToAdvance;
    private IModuleExecutor  _activeBlockingExecutor;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        _navigator = navigatorBehaviour as IGraphNavigator;

        if (graph == null)      { Debug.LogError("[DialogueRunner] Falta DialogueGraph.");       enabled = false; return; }
        if (_navigator == null) { Debug.LogError("[DialogueRunner] Falta IGraphNavigator.");      enabled = false; return; }

        // Registrar executors
        RegisterExecutor(textExecutor);
        RegisterExecutor(portraitExecutor);
        RegisterExecutor(eventExecutor);
        RegisterExecutor(audioExecutor);
        RegisterExecutor(choiceExecutor);

        // Suscribirse al evento de choice seleccionada
        if (choiceExecutor != null)
            choiceExecutor.OnChoiceSelected += OnChoiceSelected;

        // Inicializar executor que lo necesitan (ej. PortraitController necesita el grafo)
        foreach (var executor in _executors.Values)
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
        if (choiceExecutor != null && choiceExecutor.TryConsumeHotkey(out _))
            return; // choiceExecutor ya dispara OnChoiceSelected

        if (Input.GetKeyDown(advanceKey))
        {
            // Intentar fast-forward en el executor activo (ej. skip typewriter)
            if (_activeBlockingExecutor != null && _activeBlockingExecutor.TryFastForward())
                return;

            if (_nodeReadyToAdvance)
                AdvanceToNext();
        }
    }

    private async Task ShowNodeAsync(DialogueNodeData node)
    {
        _nodeReadyToAdvance     = false;
        _activeBlockingExecutor = null;

        if (node == null) { EndDialogue(); return; }

        _cts = new CancellationTokenSource();
        var ctx = new ModuleExecutionContext(node.GUID, _cts.Token);

        // Notificar inicio de nodo a todos los executors
        foreach (var executor in _executors.Values)
            executor.OnNodeBegin();

        // Ejecutar módulos en orden
        foreach (var module in node.modules)
        {
            if (module == null) continue;
            if (!_executors.TryGetValue(module.GetType(), out var executor))
            {
                Debug.LogWarning($"[DialogueRunner] Sin executor para módulo tipo '{module.GetType().Name}'. Saltando.");
                continue;
            }

            if (_cts.IsCancellationRequested) break;

            if (module.Blocks)
            {
                _activeBlockingExecutor = executor;
                try
                {
                    await executor.ExecuteAsync(module, ctx);
                }
                catch (OperationCanceledException) { break; }
                finally { _activeBlockingExecutor = null; }
            }
            else
            {
                // Fire and forget — no bloqueamos el loop de módulos
                var capturedModule   = module;
                var capturedExecutor = executor;
                _ = capturedExecutor.ExecuteAsync(capturedModule, ctx);
            }
        }

        // Si el nodo no tiene ChoiceModule, quedamos listos para avanzar
        if (!node.IsChoiceNode)
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

        portraitExecutor?.ResetAll();
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        if (choiceExecutor != null)
            choiceExecutor.OnChoiceSelected -= OnChoiceSelected;
    }

    private void RegisterExecutor(IModuleExecutor executor)
    {
        if (executor == null) return;
        _executors[executor.ModuleType] = executor;
    }
}
