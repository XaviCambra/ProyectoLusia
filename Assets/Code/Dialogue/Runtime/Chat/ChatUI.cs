using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

    [Header("Opciones (inline)")]
    [SerializeField] private ChoicesBubble      choicesBubblePrefab;
    [SerializeField] private ImageChoicesBubble imageChoicesBubblePrefab;

    private Dictionary<ChatContentType, BubblePrefabMapping> _bubbleMap;

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

    public async Task ShowTypingAsync(CharacterDefinition speaker, float seconds, CancellationToken ct)
    {
        TypingBubble instance = null;
        if (typingBubblePrefab && contentParent)
        {
            instance = Instantiate(typingBubblePrefab, contentParent);
            instance.Set(speaker);
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

    public async Task<int> ShowImageChoicesAsync(IReadOnlyList<ImageChoiceModule.ImageChoiceData> choices, CharacterDefinition speaker, CancellationToken ct)
    {
        if (!imageChoicesBubblePrefab || !contentParent) return 0;

        var bubble = Instantiate(imageChoicesBubblePrefab, contentParent);
        try
        {
            return await bubble.ShowAsync(choices, speaker, ct);
        }
        finally
        {
            if (bubble) Destroy(bubble.gameObject);
        }
    }

    public async Task<int> ShowChoicesAsync(IReadOnlyList<ChoiceModule.ChoiceData> choices, CharacterDefinition speaker, CancellationToken ct)
    {
        if (!choicesBubblePrefab || !contentParent) return 0;

        var bubble = Instantiate(choicesBubblePrefab, contentParent);
        try
        {
            return await bubble.ShowAsync(choices, speaker, ct);
        }
        finally
        {
            if (bubble) Destroy(bubble.gameObject);
        }
    }

    public void Clear()
    {
        if (contentParent)
            foreach (Transform child in contentParent)
                Destroy(child.gameObject);

    }

    // -----------------------------------------------------------------------
    // Internos
    // -----------------------------------------------------------------------

    private void ScrollToBottom()
    {
        if (scrollRect) scrollRect.verticalNormalizedPosition = 0f;
    }
}
