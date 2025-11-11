using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ChoiceUIController : MonoBehaviour, IChoiceUIController
{
    [Header("Botones (en orden)")]
    [SerializeField] private Button[] buttons;

    [SerializeField] private MonoBehaviour conditionEvaluatorRef; // arrastra aquí tu ConditionEvaluator
    private IConditionEvaluator _conditions;

    [Header("Localización (opcional)")]
    [SerializeField] private MonoBehaviour localizationRef; // arrastra aquí tu servicio
    private ILocalizationService _loc;

    [Header("Estilo")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color visitedColor = new(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private bool useAlphaForDisabled = true;
    [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.5f;

    // estado interno
    private readonly HashSet<string> _visited = new();
    private DialogueNodeData _currentNode;
    private readonly List<(Button btn, DialogueNodeData.ChoiceData choice, string port)> _map = new();

    private void Awake()
    {
        _conditions = conditionEvaluatorRef as IConditionEvaluator;
        if (_conditions == null) _conditions = FindAnyObjectByType<ConditionEvaluator>();

        _loc = localizationRef as ILocalizationService;
        if (_loc == null)
        {
            var monos = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < monos.Length; i++)
            {
                if (monos[i] is ILocalizationService svc) { _loc = svc; break; }
            }
        }
    }

    public void Init()
    {
        Hide();
    }

    public void BeginNode()
    {
        Hide();
    }

    public bool Show(DialogueNodeData node,
                 IEnumerable<DialogueNodeData.ChoiceData> candidates,
                 Action<string> onClick)
    {
        // Precondición: BeginNode() ya limpió la UI.
        _currentNode = node;

        if (node == null || !node.isChoiceNode || buttons == null || buttons.Length == 0)
            return false;

        // 1) Prepara una lista con el estado permitido/bloqueado por cada opción
        var toRender = new List<(DialogueNodeData.ChoiceData choice, string port, bool allowed)>();
        foreach (var c in candidates ?? Enumerable.Empty<DialogueNodeData.ChoiceData>())
        {
            if (c == null) continue;
            bool allowed = _conditions == null ? true : _conditions.IsAllowed(c);

            // Ocultar bloqueadas si el nodo NO quiere mostrarlas
            if (!allowed && !node.showBlockedChoices) continue;

            var port = string.IsNullOrEmpty(c.portName) ? $"choice_{toRender.Count}" : c.portName;
            toRender.Add((c, port, allowed));
        }

        // Nada que mostrar
        if (toRender.Count == 0) return false;

        // 2) Pintar en botones (hasta el máximo disponible)
        _map.Clear();
        int write = 0;

        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (!btn) continue;

            btn.onClick.RemoveAllListeners();

            if (write >= toRender.Count)
            {
                btn.gameObject.SetActive(false);
                continue;
            }

            var (choice, port, allowed) = toRender[write++];
            var label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label)
            {
                string displayText;
                if (choice.choiceUseLocalization && !string.IsNullOrEmpty(choice.choiceLocKey) && _loc != null)
                {
                    displayText = _loc.TryGet(choice.choiceLocKey, out var loc)
                                  ? loc
                                  : choice.choiceLocKey; // fallback amigable: muestra la clave
                }
                else
                {
                    displayText = string.IsNullOrEmpty(choice.choiceText) ? $"Opción {write}" : choice.choiceText;
                }
                label.text = displayText;
            }

            // color visitado / normal
            var img = btn.image;
            if (img)
            {
                img.color = _visited.Contains(Key(node.GUID, port)) ? visitedColor : normalColor;

                // si se muestran bloqueadas: bajamos alpha
                if (useAlphaForDisabled)
                    img.canvasRenderer.SetAlpha(allowed ? 1f : disabledAlpha);
            }

            btn.interactable = allowed;

            if (allowed)
            {
                btn.onClick.AddListener(() =>
                {
                    _visited.Add(Key(node.GUID, port));
                    onClick?.Invoke(port);
                });
            }

            btn.gameObject.SetActive(true);
            _map.Add((btn, choice, port));
        }

        // Oculta botones sobrantes si hay menos opciones que slots
        for (int i = write; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (!btn) continue;
            btn.onClick.RemoveAllListeners();
            btn.gameObject.SetActive(false);
        }

        return _map.Count > 0;
    }

    public void Hide()
    {
        // 1) Oculta y limpia lo que estuviera mapeado por el último Show()
        if (_map.Count > 0)
        {
            foreach (var (btn, _, _) in _map)
            {
                if (!btn) continue;
                btn.onClick.RemoveAllListeners();
                btn.gameObject.SetActive(false);
                // Debug.Log($"HIDE map -> {btn.name}");
            }
            _map.Clear();
        }

        // 2) Fallback: también oculta TODOS los botones asignados en el inspector
        //    (útil cuando Hide() se llama ANTES del primer Show(), p.ej. en BeginNode())
        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];
                if (!btn) continue;
                btn.onClick.RemoveAllListeners();
                btn.gameObject.SetActive(false);
                // Debug.Log($"HIDE fallback -> {btn.name}");
            }
        }

        _currentNode = null;
    }

    public bool TryConsumeHotkey(out string chosenPort)
    {
        chosenPort = null;
        if (_currentNode == null || _map.Count == 0) return false;

        int idx = 0;
        for (int i = 0; i < buttons.Length && idx < _map.Count; i++)
        {
            var btn = buttons[i];
            if (!btn || !btn.gameObject.activeSelf) continue;  // botón no visible
            var mapping = _map[idx++];

            // Solo consume hotkey si el botón está interactivo (opción permitida)
            if (!btn.interactable) continue;

            if (Input.GetKeyDown(KeyCode.Alpha1 + (idx - 1)) || Input.GetKeyDown(KeyCode.Keypad1 + (idx - 1)))
            {
                chosenPort = mapping.port;
                _visited.Add(Key(_currentNode.GUID, chosenPort));
                return true;
            }
        }
        return false;
    }

    private static string Key(string g, string p) => $"{g}::{p}";
}
