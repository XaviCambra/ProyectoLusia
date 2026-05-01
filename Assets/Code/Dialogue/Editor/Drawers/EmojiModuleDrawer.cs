#if UNITY_EDITOR
using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Drawer del editor para <see cref="EmojiModule"/>.</summary>
public static class EmojiModuleDrawer
{
    public static VisualElement Draw(EmojiModule m, Action onChanged)
    {
        var root = new VisualElement();
        ModuleDrawerStyles.ApplyRootPadding(root);

        // Preview del sprite
        var preview = new UnityEngine.UIElements.Image
        {
            scaleMode = ScaleMode.ScaleToFit
        };
        preview.style.width       = 64;
        preview.style.height      = 64;
        preview.style.alignSelf   = Align.Center;
        preview.style.marginTop   = 8;
        preview.style.marginBottom = 4;
        preview.sprite            = m.emoji;
        preview.style.display     = m.emoji != null ? DisplayStyle.Flex : DisplayStyle.None;
        root.Add(preview);

        // Campo de sprite
        var emojiField = new ObjectField("Emoji")
        {
            objectType = typeof(Sprite),
            value      = m.emoji
        };
        emojiField.RegisterValueChangedCallback(e =>
        {
            m.emoji               = e.newValue as Sprite;
            preview.sprite        = m.emoji;
            preview.style.display = m.emoji != null ? DisplayStyle.Flex : DisplayStyle.None;
            onChanged?.Invoke();
        });
        ModuleDrawerStyles.ApplyFieldMargins(emojiField);
        root.Add(emojiField);

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
