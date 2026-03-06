#if UNITY_EDITOR
using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

/// <summary>Drawer del editor para <see cref="PortraitModule"/>.</summary>
public static class PortraitModuleDrawer
{
    public static VisualElement Draw(PortraitModule m, Action onChanged)
    {
        var root = new VisualElement();
        root.style.paddingLeft  = 4;
        root.style.paddingRight = 4;

        // --- Perfil ---
        var profileField = new ObjectField("Profile")
        {
            objectType = typeof(CharacterProfile),
            value      = m.profileRef
        };
        profileField.RegisterValueChangedCallback(e =>
        {
            m.profileRef = e.newValue as CharacterProfile;
            m.profileId  = m.profileRef != null ? m.profileRef.ProfileId : string.Empty;
            onChanged?.Invoke();
        });
        root.Add(profileField);

        var keyField = new TextField("Portrait Key") { value = m.portraitKey };
        keyField.RegisterValueChangedCallback(e => { m.portraitKey = e.newValue; onChanged?.Invoke(); });
        root.Add(keyField);

        // --- Apariencia ---
        root.Add(MakeSeparator("Appearance"));

        var appearField = new EnumField("Mode", m.appearance);
        appearField.RegisterValueChangedCallback(e => { m.appearance = (AppearanceMode)e.newValue; onChanged?.Invoke(); });
        root.Add(appearField);

        var originField = new EnumField("Origin", m.origin);
        originField.RegisterValueChangedCallback(e => { m.origin = (Spot)e.newValue; onChanged?.Invoke(); });
        root.Add(originField);

        var targetField = new EnumField("Target", m.target);
        targetField.RegisterValueChangedCallback(e => { m.target = (Spot)e.newValue; onChanged?.Invoke(); });
        root.Add(targetField);

        var speedField = new FloatField("Move Speed") { value = m.moveSpeed };
        speedField.RegisterValueChangedCallback(e =>
        {
            m.moveSpeed = UnityEngine.Mathf.Max(0f, e.newValue);
            speedField.SetValueWithoutNotify(m.moveSpeed);
            onChanged?.Invoke();
        });
        root.Add(speedField);

        // --- Fade ---
        var fadeToggle = new Toggle("Use Fade") { value = m.useFade };
        var fadeFields = BuildFadeFields(m, onChanged);
        fadeFields.style.display = m.useFade ? DisplayStyle.Flex : DisplayStyle.None;

        fadeToggle.RegisterValueChangedCallback(e =>
        {
            m.useFade = e.newValue;
            fadeFields.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            onChanged?.Invoke();
        });
        root.Add(fadeToggle);
        root.Add(fadeFields);

        return root;
    }

    private static VisualElement BuildFadeFields(PortraitModule m, Action onChanged)
    {
        var c = new VisualElement();

        var fromField = new IntegerField("From Opacity (0-100)") { value = m.enterFromOpacity };
        fromField.RegisterValueChangedCallback(e =>
        {
            m.enterFromOpacity = UnityEngine.Mathf.Clamp(e.newValue, 0, 100);
            fromField.SetValueWithoutNotify(m.enterFromOpacity);
            onChanged?.Invoke();
        });

        var toField = new IntegerField("To Opacity (0-100)") { value = m.enterToOpacity };
        toField.RegisterValueChangedCallback(e =>
        {
            m.enterToOpacity = UnityEngine.Mathf.Clamp(e.newValue, 0, 100);
            toField.SetValueWithoutNotify(m.enterToOpacity);
            onChanged?.Invoke();
        });

        c.Add(fromField);
        c.Add(toField);
        return c;
    }

    private static Label MakeSeparator(string text)
    {
        var lbl = new Label(text);
        lbl.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
        lbl.style.marginTop = 6;
        lbl.style.marginBottom = 2;
        return lbl;
    }
}
#endif
