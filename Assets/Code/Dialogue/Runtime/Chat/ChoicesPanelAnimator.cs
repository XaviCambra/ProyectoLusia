using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra y oculta el ChoicesPanel ajustando su altura al contenido de forma inmediata.
/// </summary>
[RequireComponent(typeof(LayoutElement))]
public sealed class ChoicesPanelAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private float         fallbackHeight = 200f;
    [SerializeField] private GameObject    divider;

    private LayoutElement _layout;
    private RectTransform _parentRect;

    private void Awake()
    {
        _layout     = GetComponent<LayoutElement>();
        _parentRect = transform.parent as RectTransform;
        _layout.preferredHeight = 0f;
        if (divider) divider.SetActive(false);
    }

    public void Show()
    {
        if (contentRect) LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        _layout.preferredHeight = contentRect
            ? LayoutUtility.GetPreferredHeight(contentRect)
            : fallbackHeight;
        if (_parentRect) LayoutRebuilder.MarkLayoutForRebuild(_parentRect);
        if (divider) divider.SetActive(true);
    }

    public void Hide() => HideImmediate();

    public void HideImmediate()
    {
        _layout.preferredHeight = 0f;
        if (_parentRect) LayoutRebuilder.MarkLayoutForRebuild(_parentRect);
        if (divider) divider.SetActive(false);
    }
}
