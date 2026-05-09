#if UNITY_EDITOR
using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Drawer del editor para <see cref="ImageChoiceModule"/>.</summary>
public static class ImageChoiceModuleDrawer
{
    public static VisualElement Draw(ImageChoiceModule m, Action onChanged)
    {
        var root = new VisualElement();
        ModuleDrawerStyles.ApplyRootPadding(root);

        var choiceList = new VisualElement();
        root.Add(choiceList);

        void RebuildList()
        {
            choiceList.Clear();
            for (int i = 0; i < m.choices.Count; i++)
            {
                var choice = m.choices[i];
                int idx    = i;
                choiceList.Add(BuildChoiceItem(m, choice, idx, RebuildList, onChanged));
            }
        }

        var addBtn = new Button(() =>
        {
            m.choices.Add(new ImageChoiceModule.ImageChoiceData
            {
                portName = Guid.NewGuid().ToString("N")[..8]
            });
            RebuildList();
            onChanged?.Invoke();
        }) { text = "+ Add Image Choice" };
        addBtn.style.marginTop = 4;
        root.Add(addBtn);

        RebuildList();

        ModuleDrawerStyles.AdjustFirstAndLastMargins(root);
        return root;
    }

    private static VisualElement BuildChoiceItem(
        ImageChoiceModule module,
        ImageChoiceModule.ImageChoiceData choice,
        int index,
        Action rebuildList,
        Action onChanged)
    {
        var item = new VisualElement();
        item.style.borderBottomWidth = 1;
        item.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        item.style.marginBottom      = 4;

        // Header: label + delete button
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;

        var label = new Label($"Choice {index + 1}");
        label.style.flexGrow = 1;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;

        var deleteBtn = new Button(() =>
        {
            module.choices.RemoveAt(index);
            rebuildList();
            onChanged?.Invoke();
        }) { text = "✕" };
        deleteBtn.style.width = 24;

        header.Add(label);
        header.Add(deleteBtn);
        item.Add(header);

        // Port name (readonly display)
        var portLabel = new Label($"Port: {choice.portName}");
        portLabel.style.color    = new Color(0.6f, 0.6f, 0.6f, 1f);
        portLabel.style.fontSize = 10;
        item.Add(portLabel);

        // Preview del sprite
        var preview = new UnityEngine.UIElements.Image { scaleMode = ScaleMode.ScaleToFit };
        preview.style.width        = 64;
        preview.style.height       = 64;
        preview.style.alignSelf    = Align.Center;
        preview.style.marginTop    = 4;
        preview.style.marginBottom = 2;
        preview.sprite             = choice.sprite;
        preview.style.display      = choice.sprite != null ? DisplayStyle.Flex : DisplayStyle.None;
        item.Add(preview);

        // Campo de sprite
        var spriteField = new ObjectField("Sprite") { objectType = typeof(Sprite), value = choice.sprite };
        spriteField.RegisterValueChangedCallback(e =>
        {
            choice.sprite         = e.newValue as Sprite;
            preview.sprite        = choice.sprite;
            preview.style.display = choice.sprite != null ? DisplayStyle.Flex : DisplayStyle.None;
            onChanged?.Invoke();
        });
        ModuleDrawerStyles.ApplyFieldMargins(spriteField);
        item.Add(spriteField);

        return item;
    }
}
#endif
