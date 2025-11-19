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
        // 1) ConditionEvaluator
        if (conditionEvaluatorRef != null)
        {
            _conditions = conditionEvaluatorRef as IConditionEvaluator;
            if (_conditions == null)
            {
                Debug.LogError("[ChoiceUIController] El componente asignado en 'conditionEvaluatorRef' no implementa IConditionEvaluator.");
            }
        }

        // Fallback: si no se ha asignado nada en el inspector, buscar uno en la escena
        if (_conditions == null)
        {
            _conditions = FindAnyObjectByType<ConditionEvaluator>();
            if (_conditions == null)
            {
                Debug.LogWarning("[ChoiceUIController] No se encontró ningún ConditionEvaluator en la escena. Las opciones no comprobarán requisitos.");
            }
        }

        // 2) Localization (opcional)
        if (localizationRef != null)
        {
            _loc = localizationRef as ILocalizationService;
            if (_loc == null)
            {
                Debug.LogError("[ChoiceUIController] El componente asignado en 'localizationRef' no implementa ILocalizationService.");
            }
        }
        else
        {
            _loc = null; // sin localización → usar texto tal cual
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
        // Oculta y limpia TODOS los botones asignados en el inspector
        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];
                if (!btn) continue;

                btn.onClick.RemoveAllListeners();
                btn.gameObject.SetActive(false);
            }
        }

        _map.Clear();
        _currentNode = null;
    }

    public bool TryConsumeHotkey(out string chosenPort)
    {
        chosenPort = null;
        if (_currentNode == null || _map.Count == 0) return false;

        for (int i = 0; i < _map.Count; i++)
        {
            var (btn, _, port) = _map[i];
            if (!btn || !btn.gameObject.activeSelf || !btn.interactable)
                continue;

            // i = 0 → tecla 1, i = 1 → tecla 2, etc.
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                chosenPort = port;
                _visited.Add(Key(_currentNode.GUID, chosenPort));
                return true;
            }
        }

        return false;
    }

    private static string Key(string g, string p) => $"{g}::{p}";
}
