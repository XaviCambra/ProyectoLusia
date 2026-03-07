#if UNITY_EDITOR
using UnityEngine.UIElements;

/// <summary>Constantes de estilo compartidas por todos los drawers de módulos.</summary>
public static class ModuleDrawerStyles
{
    // ─── Contenedor raíz del drawer ──────────────────────────────────────────
    public const float RootPaddingLeft   = 4f;
    public const float RootPaddingRight  = 4f;
    public const float RootPaddingTop    = 0f;
    public const float RootPaddingBottom = 0f;

    // ─── Campos: TextField, ObjectField, FloatField, EnumField, IntegerField ─
    public const float FieldMarginTop    = 4f;
    public const float FieldMarginBottom = 4f;
    public const float FieldMarginLeft   = 0f;
    public const float FieldMarginRight  = 4f;

    // ─── Checkboxes (Toggle) ─────────────────────────────────────────────────
    public const float ToggleMarginTop    = 4f;
    public const float ToggleMarginBottom = 4f;
    public const float ToggleMarginLeft   = 0f;
    public const float ToggleMarginRight  = 0f;

    // ─── Primer y último elemento del drawer ─────────────────────────────────
    public const float FirstFieldMarginTop   = 8f;
    public const float LastFieldMarginBottom = 4f;

    // ─── Helpers ─────────────────────────────────────────────────────────────
    public static void ApplyRootPadding(VisualElement root)
    {
        root.style.paddingLeft   = RootPaddingLeft;
        root.style.paddingRight  = RootPaddingRight;
        root.style.paddingTop    = RootPaddingTop;
        root.style.paddingBottom = RootPaddingBottom;
    }

    public static void ApplyFieldMargins(VisualElement field)
    {
        field.style.marginTop    = FieldMarginTop;
        field.style.marginBottom = FieldMarginBottom;
        field.style.marginLeft   = FieldMarginLeft;
        field.style.marginRight  = FieldMarginRight;
    }

    public static void ApplyToggleMargins(Toggle toggle)
    {
        toggle.style.marginTop    = ToggleMarginTop;
        toggle.style.marginBottom = ToggleMarginBottom;
        toggle.style.marginLeft   = ToggleMarginLeft;
        toggle.style.marginRight  = ToggleMarginRight;
    }

    /// <summary>
    /// Ajusta el margen superior del primer hijo y el inferior del último hijo de <paramref name="root"/>.
    /// Llamar al final del método Draw, tras añadir todos los elementos.
    /// </summary>
    public static void AdjustFirstAndLastMargins(VisualElement root)
    {
        VisualElement first = null, last = null;
        foreach (var child in root.Children())
        {
            if (first == null) first = child;
            last = child;
        }
        if (first != null) first.style.marginTop    = FirstFieldMarginTop;
        if (last  != null) last.style.marginBottom  = LastFieldMarginBottom;
    }
}
#endif
