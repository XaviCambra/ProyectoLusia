#if UNITY_EDITOR
using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

/// <summary>Drawer del editor para <see cref="ProfileModule"/>.</summary>
public static class ProfileModuleDrawer
{
    public static VisualElement Draw(ProfileModule m, Action onChanged)
    {
        var root = new VisualElement();
        ModuleDrawerStyles.ApplyRootPadding(root);

        var profileField = new ObjectField("Profile")
        {
            objectType = typeof(CharacterDefinition),
            value      = m.profile
        };
        profileField.RegisterValueChangedCallback(e =>
        {
            m.profile = e.newValue as CharacterDefinition;
            onChanged?.Invoke();
        });
        ModuleDrawerStyles.ApplyFieldMargins(profileField);
        root.Add(profileField);

        ModuleDrawerStyles.AdjustFirstAndLastMargins(root);
        return root;
    }
}
#endif
