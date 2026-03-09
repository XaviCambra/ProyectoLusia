#if UNITY_EDITOR
using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

/// <summary>Drawer del editor para <see cref="ChatTypingModule"/>.</summary>
public static class ChatTypingModuleDrawer
{
    public static VisualElement Draw(ChatTypingModule m, Action onChanged)
    {
        var root = new VisualElement();
        ModuleDrawerStyles.ApplyRootPadding(root);

        var profileField = new ObjectField("Profile")
        {
            objectType = typeof(CharacterProfile),
            value      = m.profile
        };
        profileField.RegisterValueChangedCallback(e =>
        {
            m.profile = e.newValue as CharacterProfile;
            onChanged?.Invoke();
        });
        ModuleDrawerStyles.ApplyFieldMargins(profileField);
        root.Add(profileField);

        var durationField = new FloatField("Duration (s)") { value = m.duration };
        durationField.RegisterValueChangedCallback(e =>
        {
            m.duration = UnityEngine.Mathf.Max(0f, e.newValue);
            durationField.SetValueWithoutNotify(m.duration);
            onChanged?.Invoke();
        });
        ModuleDrawerStyles.ApplyFieldMargins(durationField);
        root.Add(durationField);

        ModuleDrawerStyles.AdjustFirstAndLastMargins(root);
        return root;
    }
}
#endif
