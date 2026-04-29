using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Implementación de referencia de <see cref="IChatPresenter"/> usando uGUI + TMP.
/// Instancia burbujas de chat, muestra el indicador de escritura y
/// presenta botones de elección al jugador.
///
/// Prefabs requeridos:
///   - <see cref="bubblePrefab"/>  : GameObject con componente <see cref="ChatBubble"/>
///   - <see cref="choiceButtonPrefab"/> : GameObject con Button + TMP_Text en un hijo
/// </summary>
public class ChatUI : MonoBehaviour, IChatPresenter
{
    [Header("Scroll")]
    [SerializeField] private ScrollRect          scrollRect;
    [SerializeField] private Transform           contentParent;
    [SerializeField] private ContentSizeListener contentSizeListener;
    [SerializeField] private ChatBubble          bubblePrefab;
    [SerializeField] private ChatBubble          ownBubblePrefab; // si se asigna, se usa para mensajes propios (isOwn=true)

    [Header("Indicador de escritura")]
    [SerializeField] private GameObject typingIndicator;
    [SerializeField] private TMP_Text   typingLabel;

    [Header("Opciones")]
    [SerializeField] private Transform choicesParent;
    [SerializeField] private Button    choiceButtonPrefab;

    private TaskCompletionSource<int> _choiceTcs;

    private void Awake()
    {
        if (contentSizeListener)
            contentSizeListener.OnSizeChanged += ScrollToBottom;
    }

    private void OnDestroy()
    {
        if (contentSizeListener)
            contentSizeListener.OnSizeChanged -= ScrollToBottom;
    }

    // -----------------------------------------------------------------------
    // IChatPresenter
    // -----------------------------------------------------------------------

    public void AddMessage(ChatEntry entry)
    {
        if (!bubblePrefab || !contentParent) return;

        var prefab = (entry.isOwn && ownBubblePrefab) ? ownBubblePrefab : bubblePrefab;
        var bubble = Instantiate(prefab, contentParent);
        bubble.Set(entry);
    }

    public async Task ShowTypingAsync(CharacterProfile profile, float seconds, CancellationToken ct)
    {
        if (typingIndicator)
        {
            if (typingLabel)
                typingLabel.text = profile != null
                    ? $"{profile.displayName} está escribiendo..."
                    : "Escribiendo...";

            typingIndicator.SetActive(true);
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
        }
        finally
        {
            if (typingIndicator) typingIndicator.SetActive(false);
        }
    }

    public async Task<int> ShowChoicesAsync(IReadOnlyList<ChoiceModule.ChoiceData> choices, CancellationToken ct)
    {
        _choiceTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        for (int i = 0; i < choices.Count; i++)
        {
            var idx = i;
            var btn = Instantiate(choiceButtonPrefab, choicesParent);
            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label) label.text = choices[i].choiceText;
            btn.onClick.AddListener(() => SelectChoice(idx));
        }

        using var reg = ct.Register(() =>
        {
            _choiceTcs?.TrySetCanceled();
            ClearChoiceButtons();
        });

        int result = await _choiceTcs.Task;
        ClearChoiceButtons();
        return result;
    }

    public void Clear()
    {
        if (contentParent)
            foreach (Transform child in contentParent)
                Destroy(child.gameObject);

        ClearChoiceButtons();

        if (typingIndicator) typingIndicator.SetActive(false);
    }

    // -----------------------------------------------------------------------
    // Internos
    // -----------------------------------------------------------------------

    private void SelectChoice(int idx)
    {
        _choiceTcs?.TrySetResult(idx);
        _choiceTcs = null;
    }

    private void ClearChoiceButtons()
    {
        if (!choicesParent) return;
        foreach (Transform child in choicesParent)
            Destroy(child.gameObject);
    }

    private void ScrollToBottom()
    {
        if (scrollRect) scrollRect.verticalNormalizedPosition = 0f;
    }
}
