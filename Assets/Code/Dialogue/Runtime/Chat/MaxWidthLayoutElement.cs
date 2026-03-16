using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sustituye a LayoutElement cuando necesitas "ancho natural del texto, máximo X".
/// Colócalo en el mismo GameObject que el TMP_Text.
/// El LayoutGroup padre lo interroga y recibe min(textWidth, maxWidth).
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class MaxWidthLayoutElement : MonoBehaviour, ILayoutElement
{
    [SerializeField] private float maxWidth = 200f;

    private TMP_Text _tmp;

    private void Awake() => _tmp = GetComponent<TMP_Text>();

    // Ancho preferido = ancho natural del texto, pero nunca supera maxWidth
    public float preferredWidth  => _tmp != null ? Mathf.Min(_tmp.preferredWidth, maxWidth) : maxWidth;
    public float preferredHeight => -1;
    public float minWidth        => -1;
    public float minHeight       => -1;
    public float flexibleWidth   => 0;
    public float flexibleHeight  => -1;
    public int   layoutPriority  => 1;

    public void CalculateLayoutInputHorizontal() { }
    public void CalculateLayoutInputVertical()   { }
}
