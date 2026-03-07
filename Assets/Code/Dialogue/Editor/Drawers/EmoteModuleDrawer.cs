#if UNITY_EDITOR
using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

/// <summary>Drawer del editor para <see cref="EmoteModule"/>.</summary>
public static class EmoteModuleDrawer
{
    public static VisualElement Draw(EmoteModule m, Action onChanged)
    {
        var root = new VisualElement();
        ModuleDrawerStyles.ApplyRootPadding(root);

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
        ModuleDrawerStyles.ApplyFieldMargins(profileField);
        root.Add(profileField);

        // --- Clip (sin clip = stop) ---
        var stopLabel   = new Label("Sin clip: detiene el emote activo") { style = { color = new UnityEngine.Color(1f, 0.6f, 0.2f) } };
        var playFields  = BuildPlayFields(m, onChanged);

        stopLabel.style.display  = m.clip == null ? DisplayStyle.Flex : DisplayStyle.None;
        playFields.style.display = m.clip != null ? DisplayStyle.Flex : DisplayStyle.None;

        var clipField = new ObjectField("Clip")
        {
            objectType = typeof(UnityEngine.AnimationClip),
            value      = m.clip
        };
        clipField.RegisterValueChangedCallback(e =>
        {
            m.clip = e.newValue as UnityEngine.AnimationClip;
            bool hasClip = m.clip != null;
            stopLabel.style.display  = hasClip ? DisplayStyle.None : DisplayStyle.Flex;
            playFields.style.display = hasClip ? DisplayStyle.Flex : DisplayStyle.None;
            onChanged?.Invoke();
        });
        ModuleDrawerStyles.ApplyFieldMargins(clipField);

        root.Add(clipField);
        root.Add(stopLabel);
        root.Add(playFields);

        ModuleDrawerStyles.AdjustFirstAndLastMargins(root);
        return root;
    }

    private static VisualElement BuildPlayFields(EmoteModule m, Action onChanged)
    {
        var c = new VisualElement();

        var speedField = new FloatField("Speed") { value = m.speed };
        speedField.RegisterValueChangedCallback(e =>
        {
            m.speed = UnityEngine.Mathf.Max(0f, e.newValue);
            speedField.SetValueWithoutNotify(m.speed);
            onChanged?.Invoke();
        });
        ModuleDrawerStyles.ApplyFieldMargins(speedField);

        var loopToggle = new Toggle("Loop") { value = m.loop };
        loopToggle.RegisterValueChangedCallback(e => { m.loop = e.newValue; onChanged?.Invoke(); });
        ModuleDrawerStyles.ApplyToggleMargins(loopToggle);

        var persistentToggle = new Toggle("Persistent") { value = m.persistent };
        persistentToggle.RegisterValueChangedCallback(e => { m.persistent = e.newValue; onChanged?.Invoke(); });
        ModuleDrawerStyles.ApplyToggleMargins(persistentToggle);

        c.Add(speedField);
        c.Add(loopToggle);
        c.Add(persistentToggle);
        return c;
    }
}
#endif
