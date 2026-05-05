using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adapta el cellSize del GridLayoutGroup al ancho disponible del contenedor.
/// Las celdas son cuadradas por defecto; ajusta <see cref="cellAspectRatio"/>
/// para celdas rectangulares (width / height).
/// </summary>
[RequireComponent(typeof(GridLayoutGroup))]
public sealed class ResponsiveGridLayout : MonoBehaviour
{
    [SerializeField] private int   columns         = 3;
    [SerializeField] private float cellAspectRatio = 1f; // width / height

    private GridLayoutGroup _grid;
    private RectTransform   _rect;

    private void Awake()
    {
        _grid = GetComponent<GridLayoutGroup>();
        _rect = GetComponent<RectTransform>();
        UpdateCellSize();
    }

    private void OnRectTransformDimensionsChange() => UpdateCellSize();

    private void UpdateCellSize()
    {
        if (!_grid || !_rect || columns <= 0) return;

        float totalSpacing  = _grid.spacing.x * (columns - 1);
        float totalPadding  = _grid.padding.left + _grid.padding.right;
        float cellWidth     = Mathf.Max(1f, (_rect.rect.width - totalPadding - totalSpacing) / columns);
        float cellHeight    = Mathf.Max(1f, cellWidth / cellAspectRatio);

        _grid.cellSize = new Vector2(cellWidth, cellHeight);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _grid = GetComponent<GridLayoutGroup>();
        _rect = GetComponent<RectTransform>();
        UpdateCellSize();
    }
#endif
}
