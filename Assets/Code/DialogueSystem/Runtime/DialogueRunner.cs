using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueRunner : MonoBehaviour
{
    [Header("Graph")]
    public DialogueGraph graph;

    [Header("Controles")]
    public KeyCode advanceKey = KeyCode.N;
    public KeyCode restartKey = KeyCode.R;

    [Header("UI (asigna tus elementos)")]
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI bodyText;
    [Tooltip("Botones de elección (hasta 4) en orden.")]
    public Button[] choiceButtons;

    public event Action OnDialogueEnd;

    private readonly Dictionary<string, DialogueNodeData> _nodeByGuid = new();
    private readonly Dictionary<(string fromGuid, string fromPort), string> _edgeLookup = new();
    private readonly Dictionary<string, int> _incomingCount = new();

    private DialogueNodeData _current;
    private bool _waitingChoice;

    private void Awake()
    {
        if (!ValidateGraph()) return;
        BuildLookups();
        StartDialogue();
    }

    private void Update()
    {
        if (_current == null) return;

        if (Input.GetKeyDown(restartKey))
        {
            StartDialogue();
            return;
        }

        if (_waitingChoice) return;

        if (!_current.isChoiceNode && Input.GetKeyDown(advanceKey))
        {
            GoNext();
        }
    }

    private bool ValidateGraph()
    {
        if (graph == null)
        {
            Debug.LogError("[DialogueRunner] No hay DialogueGraph asignado.");
            return false;
        }

        _nodeByGuid.Clear();

        foreach (var n in graph.Nodes)
        {
            if (n == null || string.IsNullOrEmpty(n.GUID)) continue;
            if (!_nodeByGuid.ContainsKey(n.GUID))
                _nodeByGuid.Add(n.GUID, n);
        }

        if (_nodeByGuid.Count == 0)
        {
            Debug.LogError("[DialogueRunner] El graph no contiene nodos válidos.");
            return false;
        }

        return true;
    }

    private void BuildLookups()
    {
        _edgeLookup.Clear();
        _incomingCount.Clear();

        foreach (var guid in _nodeByGuid.Keys)
            _incomingCount[guid] = 0;

        foreach (var e in graph.Edges)
        {
            if (e == null || string.IsNullOrEmpty(e.fromNodeGUID) || string.IsNullOrEmpty(e.toNodeGUID))
                continue;

            var fromPort = string.IsNullOrEmpty(e.fromPortName) ? "Next" : e.fromPortName;
            var edgeKey = (fromGuid: e.fromNodeGUID, fromPort: fromPort);

            if (_edgeLookup.ContainsKey(edgeKey))
            {
                Debug.LogWarning($"[DialogueRunner] Puerto duplicado: {edgeKey.fromGuid}:{edgeKey.fromPort}. " +
                                 $"Ya existe conexión hacia GUID={_edgeLookup[edgeKey]}. Ignorando adicional.");
                continue;
            }

            _edgeLookup[edgeKey] = e.toNodeGUID;
            _incomingCount[e.toNodeGUID] = _incomingCount.GetValueOrDefault(e.toNodeGUID) + 1;
        }
    }

    private void StartDialogue()
    {
        DialogueNodeData start = null;

        // 1) Prioriza flag isStart
        var starts = _nodeByGuid.Values.Where(n => n.isStart).ToList();
        if (starts.Count > 1)
            Debug.LogWarning($"[DialogueRunner] Hay {starts.Count} nodos marcados como inicio; se usará el primero.");

        if (starts.Count >= 1)
            start = starts[0];

        // 2) Si no hay isStart, elige uno sin entradas
        if (start == null)
            start = _nodeByGuid.Values.FirstOrDefault(n => _incomingCount.TryGetValue(n.GUID, out var c) && c == 0);

        // 3) Fallback absoluto: el primero que exista
        if (start == null)
            start = _nodeByGuid.Values.First();

        SetCurrent(start);
    }

    private void SetCurrent(DialogueNodeData node)
    {
        _current = node;
        _waitingChoice = false;

        if (_current == null)
        {
            EndDialogue();
            return;
        }

        if (!string.IsNullOrEmpty(_current.eventKey))
            GlobalDialogueEvents.Fire(_current.eventKey);

        if (speakerText) speakerText.text = GetSpeakerName(_current);
        if (bodyText) bodyText.text = GetNodeText(_current);

        if (_current.isChoiceNode)
            ShowChoices(_current);
        else
            HideChoices();
    }

    private void ShowChoices(DialogueNodeData node)
    {
        _waitingChoice = true;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            var btn = choiceButtons[i];
            if (btn == null) continue;
            btn.onClick.RemoveAllListeners();

            if (node.choices != null && i < node.choices.Count)
            {
                var choice = node.choices[i];
                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.text = string.IsNullOrEmpty(choice.choiceText) ? $"Opción {i + 1}" : choice.choiceText;

                var port = string.IsNullOrEmpty(choice.portName) ? $"choice_{i}" : choice.portName;
                btn.onClick.AddListener(() => OnChoiceSelected(node.GUID, port));
                btn.gameObject.SetActive(true);
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }

    private void HideChoices()
    {
        foreach (var b in choiceButtons)
            if (b) b.gameObject.SetActive(false);
    }

    private void OnChoiceSelected(string fromGuid, string fromPort)
    {
        _waitingChoice = false;
        var key = (fromGuid, fromPort);

        if (_edgeLookup.TryGetValue(key, out var toGuid) && _nodeByGuid.TryGetValue(toGuid, out var toNode))
            SetCurrent(toNode);
        else
            EndDialogue();
    }

    private void GoNext()
    {
        if (_current == null) return;

        var key = (_current.GUID, "Next");
        if (_edgeLookup.TryGetValue(key, out var toGuid) && _nodeByGuid.TryGetValue(toGuid, out var toNode))
        {
            SetCurrent(toNode);
            return;
        }

        var fallback = _edgeLookup.FirstOrDefault(kv => kv.Key.fromGuid == _current.GUID).Value;
        if (!string.IsNullOrEmpty(fallback) && _nodeByGuid.TryGetValue(fallback, out var to2))
            SetCurrent(to2);
        else
            EndDialogue();
    }

    private void EndDialogue()
    {
        HideChoices();
        if (speakerText) speakerText.text = "";
        if (bodyText) bodyText.text = "<i>(Fin del diálogo)</i>";
        _current = null;
        _waitingChoice = false;
        OnDialogueEnd?.Invoke();
    }

    // --- Helpers de compatibilidad de nombres ---
    private static string GetNodeText(object node)
        => TryGetStringField(node, "text", "lineText", "dialogueText", "dialogText", "content", "body");

    private static string GetSpeakerName(object node)
        => TryGetStringField(node, "speakerName", "speaker", "character", "name");

    private static string TryGetStringField(object obj, params string[] names)
    {
        if (obj == null) return "";
        var t = obj.GetType();
        foreach (var n in names)
        {
            var f = t.GetField(n);
            if (f != null && f.FieldType == typeof(string))
                return (string)(f.GetValue(obj) ?? "");
            var p = t.GetProperty(n);
            if (p != null && p.PropertyType == typeof(string))
                return (string)(p.GetValue(obj) ?? "");
        }
        return "";
    }
}
