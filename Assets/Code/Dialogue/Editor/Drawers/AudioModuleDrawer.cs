#if UNITY_EDITOR
using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

/// <summary>Drawer del editor para <see cref="AudioModule"/>.</summary>
public static class AudioModuleDrawer
{
    public static VisualElement Draw(AudioModule m, Action onChanged)
    {
        var root = new VisualElement();
        ModuleDrawerStyles.ApplyRootPadding(root);

        var clipField = new ObjectField("Clip")
        {
            objectType = typeof(UnityEngine.AudioClip),
            value      = m.clip
        };
        clipField.RegisterValueChangedCallback(e =>
        {
            m.clip = e.newValue as UnityEngine.AudioClip;
            onChanged?.Invoke();
        });
        ModuleDrawerStyles.ApplyFieldMargins(clipField);
        root.Add(clipField);

        var volField = new FloatField("Volume (0-1)") { value = m.volume };
        volField.RegisterValueChangedCallback(e =>
        {
            m.volume = UnityEngine.Mathf.Clamp01(e.newValue);
            volField.SetValueWithoutNotify(m.volume);
            onChanged?.Invoke();
        });
        ModuleDrawerStyles.ApplyFieldMargins(volField);
        root.Add(volField);

        var waitToggle = new Toggle("Wait for Completion") { value = m.waitForCompletion };
        waitToggle.RegisterValueChangedCallback(e => { m.waitForCompletion = e.newValue; onChanged?.Invoke(); });
        ModuleDrawerStyles.ApplyToggleMargins(waitToggle);
        root.Add(waitToggle);

        ModuleDrawerStyles.AdjustFirstAndLastMargins(root);
        return root;
    }
}
#endif
