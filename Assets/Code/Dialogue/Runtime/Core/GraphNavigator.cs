using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// =============================================================
// Proyecto Kimera — GraphNavigator
// Encapsula toda la navegación del grafo y la resolución de textos.
// Requiere la interfaz IGraphNavigator ya definida (p. ej., en DialogueRunner.cs).
// =============================================================


[DisallowMultipleComponent]
public sealed class GraphNavigator : MonoBehaviour, IGraphNavigator
{
    [Header("Servicios (opcionales)")]
    [SerializeField] private MonoBehaviour localizationServiceRef; // ILocalizationService

    [Header("Override de inicio (opcional)")]
    [SerializeField] private string preferredStartId;
    [SerializeField] private string preferredStartGuid;
    public string PreferredStartId { get => preferredStartId; set => preferredStartId = value; }
    public string PreferredStartGuid { get => preferredStartGuid; set => preferredStartGuid = value; }

    private DialogueGraph _graph;
    private ILocalizationService _loc;

    // Lookups
    private readonly Dictionary<string, DialogueNodeData> _byGuid = new();
    private readonly Dictionary<(string fromGuid, string fromPort), string> _edges = new();
    private readonly Dictionary<string, int> _incomingCount = new();

    #region IGraphNavigator

    public void Init(DialogueGraph graph)
    {
        _graph = graph;
        _loc = (localizationServiceRef as ILocalizationService) ?? FindAnyObjectByType<CsvLocalizationService>();

        if (!ValidateGraph())
            throw new InvalidOperationException("[GraphNavigator] DialogueGraph inválido o vacío.");

        BuildLookups();
    }

    public DialogueNodeData StartNode()
    {
        DialogueNodeData start = null;

        // 0) Overrides externos correctos (y marcados como inicio)
        if (!string.IsNullOrEmpty(preferredStartGuid) && _byGuid.TryGetValue(preferredStartGuid, out var byGuid) && byGuid.isStart)
            start = byGuid;
        else if (!string.IsNullOrEmpty(preferredStartId))
            start = _byGuid.Values.FirstOrDefault(n => n.isStart && string.Equals(n.startId, preferredStartId, StringComparison.OrdinalIgnoreCase));

        // 1) Sin override: primer nodo marcado como inicio
        if (start == null)
        {
            var starts = _byGuid.Values.Where(n => n.isStart).ToList();
            if (starts.Count >= 1) start = starts[0];
        }

        // 2) Sin nodos de inicio: cualquiera sin entradas
        if (start == null)
            start = _byGuid.Values.FirstOrDefault(n => _incomingCount.TryGetValue(n.GUID, out var c) && c == 0);

        // 3) Fallback: primero del grafo
        if (start == null)
            start = _byGuid.Values.First();

        return start;
    }

    public DialogueNodeData NextFrom(DialogueNodeData node, string portName = "Next")
    {
        if (node == null) return null;
        var port = string.IsNullOrEmpty(portName) ? "Next" : portName;
        var key = (node.GUID, port);

        if (_edges.TryGetValue(key, out var toGuid) && _byGuid.TryGetValue(toGuid, out var toNode))
            return toNode;

        // Fallback: cualquier arista saliente desde node
        var fallback = _edges.FirstOrDefault(kv => kv.Key.fromGuid == node.GUID).Value;
        return (!string.IsNullOrEmpty(fallback) && _byGuid.TryGetValue(fallback, out var to2)) ? to2 : null;
    }

    string IGraphNavigator.ResolveSpeaker(DialogueNodeData node) => node?.speakerName ?? string.Empty;

    public string ResolveBody(DialogueNodeData node)
    {
        if (node == null) return string.Empty;

        if (node.localization && !string.IsNullOrEmpty(node.locKey) && _loc != null)
        {
            if (_loc.TryGet(node.locKey, out var localizedText))
                return localizedText;

            Debug.LogWarning($"[GraphNavigator] Clave de localización no encontrada: {node.locKey}");
        }

        return string.IsNullOrEmpty(node.lineText) ? string.Empty : node.lineText;
    }

    public void RaiseEnterEvents(DialogueNodeData node)
    {
        if (node == null) return;
        if (!string.IsNullOrEmpty(node.eventKey))
        {
            try
            {
                var payload = node.BuildEventPayload();
                GlobalDialogueEvents.Fire(payload);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GraphNavigator] Excepción en RaiseEnterEvents: {ex.Message}");
            }
        }
    }

    #endregion

    #region Lookups y validación

    private bool ValidateGraph()
    {
        if (_graph == null)
        {
            Debug.LogError("[GraphNavigator] No hay DialogueGraph asignado en Init().");
            return false;
        }

        _byGuid.Clear();
        foreach (var n in _graph.Nodes)
        {
            if (n == null || string.IsNullOrEmpty(n.GUID)) continue;
            if (!_byGuid.ContainsKey(n.GUID))
                _byGuid.Add(n.GUID, n);
        }

        if (_byGuid.Count == 0)
        {
            Debug.LogError("[GraphNavigator] El graph no contiene nodos válidos.");
            return false;
        }
        return true;
    }

    private void BuildLookups()
    {
        _edges.Clear();
        _incomingCount.Clear();

        foreach (var guid in _byGuid.Keys)
            _incomingCount[guid] = 0;

        foreach (var e in _graph.Edges)
        {
            if (e == null || string.IsNullOrEmpty(e.fromNodeGUID) || string.IsNullOrEmpty(e.toNodeGUID))
                continue;

            var fromPort = string.IsNullOrEmpty(e.fromPortName) ? "Next" : e.fromPortName;
            var edgeKey = (fromGuid: e.fromNodeGUID, fromPort: fromPort);

            if (_edges.ContainsKey(edgeKey))
            {
                Debug.LogWarning($"[GraphNavigator] Puerto duplicado: {edgeKey.fromGuid}:{edgeKey.fromPort}. Ya existe conexión hacia GUID={_edges[edgeKey]}. Ignorando adicional.");
                continue;
            }

            _edges[edgeKey] = e.toNodeGUID;
            _incomingCount[e.toNodeGUID] = _incomingCount.GetValueOrDefault(e.toNodeGUID) + 1;
        }
    }

    #endregion
}