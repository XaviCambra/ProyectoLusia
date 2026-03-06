#if UNITY_EDITOR
using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

/// <summary>Drawer del editor para <see cref="TextModule"/>.</summary>
public static class TextModuleDrawer
{
    public static VisualElement Draw(TextModule m, Action onChanged)
    {
        var root = new VisualElement();
        root.style.paddingLeft  = 4;
        root.style.paddingRight = 4;

        // Speaker Name
        var speakerField = new TextField("Speaker") { value = m.speakerName };
        speakerField.RegisterValueChangedCallback(e => { m.speakerName = e.newValue; onChanged?.Invoke(); });
        root.Add(speakerField);

        // Localization toggle + text/locKey
        var locToggle = new Toggle("Use Localization") { value = m.useLocalization };
        var textArea  = BuildTextArea(m, onChanged);
        var locKey    = BuildLocKeyField(m, onChanged);

        void UpdateLocVisibility()
        {
            textArea.style.display = m.useLocalization ? DisplayStyle.None : DisplayStyle.Flex;
            locKey.style.display   = m.useLocalization ? DisplayStyle.Flex : DisplayStyle.None;
        }

        locToggle.RegisterValueChangedCallback(e =>
        {
            m.useLocalization = e.newValue;
            UpdateLocVisibility();
            onChanged?.Invoke();
        });

        root.Add(locToggle);
        root.Add(textArea);
        root.Add(locKey);
        UpdateLocVisibility();

        // Typewriter
        var twToggle = new Toggle("Typewriter") { value = m.useTypewriter };
        var twFields = BuildTypewriterFields(m, onChanged);
        twFields.style.display = m.useTypewriter ? DisplayStyle.Flex : DisplayStyle.None;

        twToggle.RegisterValueChangedCallback(e =>
        {
            m.useTypewriter = e.newValue;
            twFields.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            onChanged?.Invoke();
        });

        root.Add(twToggle);
        root.Add(twFields);

        return root;
    }

    private static VisualElement BuildTextArea(TextModule m, Action onChanged)
    {
        var field = new TextField("Text") { value = m.text, multiline = true };
        field.style.whiteSpace = WhiteSpace.Normal;
        field.RegisterValueChangedCallback(e => { m.text = e.newValue; onChanged?.Invoke(); });
        return field;
    }

    private static VisualElement BuildLocKeyField(TextModule m, Action onChanged)
    {
        var field = new TextField("Loc Key") { value = m.locKey };
        field.RegisterValueChangedCallback(e => { m.locKey = e.newValue; onChanged?.Invoke(); });
        return field;
    }

    private static VisualElement BuildTypewriterFields(TextModule m, Action onChanged)
    {
        var container = new VisualElement();

        var profileField = new ObjectField("Profile Override")
        {
            objectType = typeof(TypewriterProfile),
            value      = m.profileOverride
        };
        profileField.RegisterValueChangedCallback(e =>
        {
            m.profileOverride = e.newValue as TypewriterProfile;
            onChanged?.Invoke();
        });

        container.Add(profileField);
        return container;
    }
}
#endif
