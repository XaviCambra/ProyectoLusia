using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
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

    // --- Typewriter ---
    private readonly TypewriterService _typewriter = new();
    private CancellationTokenSource _twCts;

    private DialogueNodeData _current;
    private bool _waitingChoice;

    // Profile character service
    private ICharacterProfileService _profiles;

    [Header("Localización (opcional)")]
    [SerializeField] private MonoBehaviour localizationServiceRef;
    private ILocalizationService _loc;

    // Referencias UI (ajústalas a tu caso real)
    [Header("UI (opcional)")]
    //[SerializeField] private Text nameText;        // o TMP_Text si usas TMP
    [SerializeField] private Image portraitImage;  // si tienes retrato en UI

    private void Awake()
    {
        // 1) Resolver servicio de perfiles primero
        // REVISAR
        _profiles = FindObjectOfType<CharacterProfileService>();

        // resolver servicio de localización
        _loc = (localizationServiceRef as ILocalizationService) ?? FindAnyObjectByType<CsvLocalizationService>();

        // 2) Validar y arrancar diálogo
        if (!ValidateGraph()) return;
        BuildLookups();
        StartDialogue();
    }

    private void OnDestroy()
    {
        _twCts?.Cancel();
        _twCts?.Dispose();
        _twCts = null;
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

    private void ApplyNodeToUI(DialogueNodeData node)
    {
        if (portraitImage == null) return; // si no usas retratos en UI, no hacemos nada
        Debug.LogWarning("HAY PORTRAITIMAGE");
        Sprite sprite = null;

        if (node != null && _profiles != null && !string.IsNullOrEmpty(node.profileId))
        {
            Debug.LogWarning("PASA LOS VERIFICADORES");

            var profile = _profiles.GetById(node.profileId);
            if (profile != null)
            {
                // 1) retrato pedido explícito por el nodo
                if (!string.IsNullOrEmpty(node.portraitKey))
                    sprite = profile.GetPortraitByKey(node.portraitKey);

                // 2) si no hay, intenta "Default"
                if (sprite == null)
                    sprite = profile.GetPortraitByKey("Default");

                // 3) si sigue sin haber, coge el primero que exista
                if (sprite == null)
                {
                    var firstKey = profile.GetPortraitKeys().FirstOrDefault();
                    if (!string.IsNullOrEmpty(firstKey))
                        sprite = profile.GetPortraitByKey(firstKey);
                }
            }
        }

        portraitImage.sprite = sprite;
        portraitImage.enabled = sprite != null; // oculta la imagen si no hay sprite
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

        //if (speakerText) speakerText.text = GetSpeakerName(_current);
        //if (bodyText) bodyText.text = ResolveBodyText(_current);
        // REMPLAZADO
        if (speakerText) speakerText.text = GetSpeakerName(_current);
        DisplayNodeBodyAsync(_current); // <- animación o instantáneo según flag del nodo

        ApplyNodeToUI(_current);

        if (_current.isChoiceNode)
            ShowChoices(_current);
        else
            HideChoices();
    }

    private async void DisplayNodeBodyAsync(DialogueNodeData node)
    {
        if (bodyText == null)
            return;

        // Texto resuelto (localización incluida)
        string nodeText = ResolveBodyText(node);

        // Si no hay Typewriter, mostrar instantáneo
        // (si tu DialogueNodeData aún no tiene useTypewriter, añade ese bool en tu modelo)
        if (node == null || !node.useTypewriter)
        {
            bodyText.SetText(nodeText);
            return;
        }

        // Preparar CTS y cancelar la animación previa si la hubiera
        _twCts?.Cancel();
        _twCts?.Dispose();
        _twCts = new CancellationTokenSource();

        // Construir perfil desde los overrides del nodo (mínimo y claro)
        var p = ScriptableObject.CreateInstance<TypewriterProfile>();
        p.secondsPerChar = node.tw.secondsPerChar;
        p.globalSpeed = node.tw.globalSpeed;
        p.respectRichText = node.tw.respectRichText;
        p.minimalWhitespaceDelay = node.tw.minimalWhitespaceDelay;

        // pausas comunes (si no las tienes en tu struct, elimínalas o añádelas)
        p.commaPct = node.tw.commaPct;
        p.periodPct = node.tw.periodPct;
        p.ellipsisPct = node.tw.ellipsisPct;

        try
        {
            await _typewriter.RunAsync(nodeText, p, bodyText, null, _twCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Cambio de nodo/skip: ignorar
        }
    }


    private string ResolveBodyText(DialogueNodeData node)
    {
        if (node == null) return string.Empty; // seguridad

        // si el nodo usa clave localizada
        if (node.localization && !string.IsNullOrEmpty(node.locKey) && _loc != null)
        {
            if (_loc.TryGet(node.locKey, out var localizedText))
                return localizedText;
            else
                Debug.LogWarning($"[DialogueRunner] Clave de localización no encontrada: {node.locKey}");
        }

        return GetNodeText(node);
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

        if (portraitImage)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }

        _current = null;
        _waitingChoice = false;
        OnDialogueEnd?.Invoke();
    }

    // --- Helpers de compatibilidad de nombres ---
    private static string GetNodeText(object node)
        => TryGetStringField(node, "text", "lineText", "dialogueText", "dialogText", "content", "body");

    private static string GetSpeakerName(object node)
        => TryGetStringField(node, "speakerName", "speaker", "character", "name");
    //private string GetSpeakerName(DialogueNodeData node)
    //{
    //    if (node == null) return string.Empty;

    //    if (_profiles != null && !string.IsNullOrEmpty(node.profileId))
    //    {
    //        var profile = _profiles.GetById(node.profileId);
    //        if (profile != null && !string.IsNullOrEmpty(profile.displayName))
    //            return profile.displayName; // prioriza el nombre del perfil
    //    }

    //    return node.speakerName; // fallback al del nodo
    //}

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
