using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Implementación de referencia de <see cref="IChatPresenter"/> usando uGUI + TMP.
/// Instancia burbujas de chat según <see cref="ChatContentType"/>, muestra el indicador
/// de escritura y presenta botones de elección al jugador.
///
/// Configura <see cref="bubblePrefabs"/> en el inspector: una entrada por cada
/// <see cref="ChatContentType"/> que quieras soportar (Text, Emoji, Image…).
/// Añadir un nuevo tipo no requiere cambios de código, solo un nuevo prefab y una entrada en la lista.
/// </summary>
public class ChatUI : MonoBehaviour, IChatPresenter
{
    [Serializable]
    public class BubblePrefabMapping
    {
        public ChatContentType contentType;
        public ChatBubble      prefab;
        [Tooltip("Si se asigna, se usa para mensajes propios (isOwn = true).")]
        public ChatBubble      ownPrefab;
    }

    [Header("Scroll")]
    [SerializeField] private ScrollRect          scrollRect;
    [SerializeField] private Transform           contentParent;
    [SerializeField] private ContentSizeListener contentSizeListener;

    [Header("Burbujas")]
    [SerializeField] private List<BubblePrefabMapping> bubblePrefabs = new();

    [Header("Indicador de escritura")]
    [SerializeField] private TypingBubble typingBubblePrefab;

    [Header("Opciones")]
    [SerializeField] private Transform            choicesParent;
    [SerializeField] private Button               choiceButtonPrefab;
    [SerializeField] private ChoicesPanelAnimator choicesAnimator;

    private Dictionary<ChatContentType, BubblePrefabMapping> _bubbleMap;
    private TaskCompletionSource<int>                        _choiceTcs;

    private void Awake()
    {
        _bubbleMap = new Dictionary<ChatContentType, BubblePrefabMapping>(bubblePrefabs.Count);
        foreach (var mapping in bubblePrefabs)
            _bubbleMap[mapping.contentType] = mapping;

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
        if (!contentParent) return;
        if (!_bubbleMap.TryGetValue(entry.contentType, out var mapping)) return;

        var prefab = (entry.isOwn && mapping.ownPrefab) ? mapping.ownPrefab : mapping.prefab;
        if (!prefab) return;

        var bubble = Instantiate(prefab, contentParent);
        bubble.Set(entry);
    }

    public async Task ShowTypingAsync(CharacterProfile profile, float seconds, CancellationToken ct)
    {
        TypingBubble instance = null;
        if (typingBubblePrefab && contentParent)
        {
            instance = Instantiate(typingBubblePrefab, contentParent);
            instance.Set(profile);
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
        }
        finally
        {
            if (instance) Destroy(instance.gameObject);
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

        choicesAnimator?.Show();

        using var reg = ct.Register(() =>
        {
            _choiceTcs?.TrySetCanceled();
            if (choicesAnimator) choicesAnimator.HideImmediate();
            ClearChoiceButtons();
        });

        int result = await _choiceTcs.Task;
        choicesAnimator?.Hide();
        ClearChoiceButtons();
        return result;
    }

    public void Clear()
    {
        if (contentParent)
            foreach (Transform child in contentParent)
                Destroy(child.gameObject);

        ClearChoiceButtons();
        choicesAnimator?.HideImmediate();
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
