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

        // --- Stop toggle ---
        var stopToggle = new Toggle("Stop Animation") { value = m.stopAnimation };
        var animFields = BuildAnimFields(m, onChanged);
        animFields.style.display = m.stopAnimation ? DisplayStyle.None : DisplayStyle.Flex;

        stopToggle.RegisterValueChangedCallback(e =>
        {
            m.stopAnimation = e.newValue;
            animFields.style.display = e.newValue ? DisplayStyle.None : DisplayStyle.Flex;
            onChanged?.Invoke();
        });
        root.Add(stopToggle);
        root.Add(animFields);

        return root;
    }

    private static VisualElement BuildAnimFields(EmoteModule m, Action onChanged)
    {
        var c = new VisualElement();

        var clipField = new ObjectField("Clip")
        {
            objectType = typeof(UnityEngine.AnimationClip),
            value      = m.clip
        };
        clipField.RegisterValueChangedCallback(e =>
        {
            m.clip = e.newValue as UnityEngine.AnimationClip;
            onChanged?.Invoke();
        });

        var speedField = new FloatField("Speed") { value = m.speed };
        speedField.RegisterValueChangedCallback(e =>
        {
            m.speed = UnityEngine.Mathf.Max(0f, e.newValue);
            speedField.SetValueWithoutNotify(m.speed);
            onChanged?.Invoke();
        });

        var loopToggle = new Toggle("Loop") { value = m.loop };
        loopToggle.RegisterValueChangedCallback(e => { m.loop = e.newValue; onChanged?.Invoke(); });

        c.Add(clipField);
        c.Add(speedField);
        c.Add(loopToggle);
        return c;
    }
}
#endif
