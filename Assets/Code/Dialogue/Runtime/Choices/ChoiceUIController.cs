using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ChoiceUIController : MonoBehaviour, IChoiceUIController
{
    [Header("Botones (en orden)")]
    [SerializeField] private Button[] buttons;

    [SerializeField] private MonoBehaviour conditionEvaluatorRef;
    private IConditionEvaluator _conditions;

    [Header("Localización (opcional)")]
    [SerializeField] private MonoBehaviour localizationRef;
    private ILocalizationService _loc;

    [Header("Estilo")]
    [SerializeField] private Color normalColor  = Color.white;
    [SerializeField] private Color visitedColor = new(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private bool  useAlphaForDisabled = true;
    [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.5f;

    // Estado interno
    private readonly HashSet<string> _visited = new();
    private string _currentNodeGuid;
    private readonly List<(Button btn, ChoiceModule.ChoiceData choice, string port)> _map = new();

    private void Awake()
    {
        if (conditionEvaluatorRef != null)
        {
            _conditions = conditionEvaluatorRef as IConditionEvaluator;
        }

        if (_conditions == null)
        {
            _conditions = FindAnyObjectByType<ConditionEvaluator>();
        }

        if (localizationRef != null)
        {
            _loc = localizationRef as ILocalizationService;
        }
    }

    public void Init()      => Hide();
    public void BeginNode() => Hide();

    public bool Show(ChoiceModule module, string nodeGuid, Action<string> onClick)
    {
        _currentNodeGuid = nodeGuid;

        if (module == null || module.choices == null || module.choices.Count == 0
            || buttons == null || buttons.Length == 0)
            return false;

        var toRender = new System.Collections.Generic.List<(ChoiceModule.ChoiceData choice, string port, bool allowed)>();
        foreach (var c in module.choices)
        {
            if (c == null) continue;
            bool allowed = _conditions == null || _conditions.IsAllowed(c);
            if (!allowed && !module.showBlockedChoices) continue;
            var port = string.IsNullOrEmpty(c.portName) ? $"choice_{toRender.Count}" : c.portName;
            toRender.Add((c, port, allowed));
        }

        if (toRender.Count == 0) return false;

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
                    displayText = _loc.TryGet(choice.choiceLocKey, out var loc) ? loc : choice.choiceLocKey;
                else
                    displayText = string.IsNullOrEmpty(choice.choiceText) ? $"Opción {write}" : choice.choiceText;
                label.text = displayText;
            }

            var img = btn.image;
            if (img)
            {
                img.color = _visited.Contains(Key(nodeGuid, port)) ? visitedColor : normalColor;
                if (useAlphaForDisabled)
                    img.canvasRenderer.SetAlpha(allowed ? 1f : disabledAlpha);
            }

            btn.interactable = allowed;

            if (allowed)
            {
                var capturedPort = port;
                btn.onClick.AddListener(() =>
                {
                    _visited.Add(Key(nodeGuid, capturedPort));
                    onClick?.Invoke(capturedPort);
                });
            }

            btn.gameObject.SetActive(true);
            _map.Add((btn, choice, port));
        }

        return _map.Count > 0;
    }

    public void Hide()
    {
        if (buttons != null)
        {
            foreach (var btn in buttons)
            {
                if (!btn) continue;
                btn.onClick.RemoveAllListeners();
                btn.gameObject.SetActive(false);
            }
        }
        _map.Clear();
        _currentNodeGuid = null;
    }

    public bool TryConsumeHotkey(out string chosenPort)
    {
        chosenPort = null;
        if (_currentNodeGuid == null || _map.Count == 0) return false;

        for (int i = 0; i < _map.Count; i++)
        {
            var (btn, _, port) = _map[i];
            if (!btn || !btn.gameObject.activeSelf || !btn.interactable) continue;

            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                chosenPort = port;
                _visited.Add(Key(_currentNodeGuid, chosenPort));
                return true;
            }
        }
        return false;
    }

    private static string Key(string g, string p) => $"{g}::{p}";
}
