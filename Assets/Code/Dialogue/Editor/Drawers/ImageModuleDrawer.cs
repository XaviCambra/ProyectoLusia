#if UNITY_EDITOR
using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Drawer del editor para <see cref="ImageModule"/>.</summary>
public static class ImageModuleDrawer
{
    public static VisualElement Draw(ImageModule m, Action onChanged)
    {
        var root = new VisualElement();
        ModuleDrawerStyles.ApplyRootPadding(root);

        // Preview del sprite
        var preview = new UnityEngine.UIElements.Image
        {
            scaleMode = ScaleMode.ScaleToFit
        };
        preview.style.width        = 80;
        preview.style.height       = 80;
        preview.style.alignSelf    = Align.Center;
        preview.style.marginTop    = 8;
        preview.style.marginBottom = 4;
        preview.sprite             = m.image;
        preview.style.display      = m.image != null ? DisplayStyle.Flex : DisplayStyle.None;
        root.Add(preview);

        // Campo de sprite
        var imageField = new ObjectField("Image")
        {
            objectType = typeof(Sprite),
            value      = m.image
        };
        imageField.RegisterValueChangedCallback(e =>
        {
            m.image               = e.newValue as Sprite;
            preview.sprite        = m.image;
            preview.style.display = m.image != null ? DisplayStyle.Flex : DisplayStyle.None;
            onChanged?.Invoke();
        });
        ModuleDrawerStyles.ApplyFieldMargins(imageField);
        root.Add(imageField);

        // Lado (propio / ajeno)
        var ownToggle = new Toggle("Own Message") { value = m.isOwn };
        ownToggle.RegisterValueChangedCallback(e => { m.isOwn = e.newValue; onChanged?.Invoke(); });
        ModuleDrawerStyles.ApplyToggleMargins(ownToggle);
        root.Add(ownToggle);

        ModuleDrawerStyles.AdjustFirstAndLastMargins(root);
        return root;
    }
}
#endif
