using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra y oculta el ChoicesPanel activando/desactivando su GameObject.
/// Cuando está inactivo no ocupa espacio en el layout del padre.
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
        if (divider) divider.SetActive(false);
    }

    public void Show()
    {
        // SetActive(true) llama a Awake si aún no se había ejecutado
        gameObject.SetActive(true);

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
        if (divider) divider.SetActive(false);
        gameObject.SetActive(false);
        if (_parentRect) LayoutRebuilder.MarkLayoutForRebuild(_parentRect);
    }
}
