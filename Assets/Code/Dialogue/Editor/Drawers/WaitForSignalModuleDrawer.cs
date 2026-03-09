#if UNITY_EDITOR
using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

/// <summary>Drawer del editor para <see cref="WaitForSignalModule"/>.</summary>
public static class WaitForSignalModuleDrawer
{
    public static VisualElement Draw(WaitForSignalModule m, Action onChanged)
    {
        var root = new VisualElement();
        ModuleDrawerStyles.ApplyRootPadding(root);

        var signalField = new ObjectField("Signal")
        {
            objectType = typeof(SignalSO),
            value      = m.signal
        };
        signalField.RegisterValueChangedCallback(e =>
        {
            m.signal = e.newValue as SignalSO;
            onChanged?.Invoke();
        });
        ModuleDrawerStyles.ApplyFieldMargins(signalField);
        root.Add(signalField);

        ModuleDrawerStyles.AdjustFirstAndLastMargins(root);
        return root;
    }
}
#endif
