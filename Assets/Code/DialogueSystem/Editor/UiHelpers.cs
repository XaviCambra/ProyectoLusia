#if UNITY_EDITOR
using System;
using UnityEngine.UIElements;

public static class UiHelpers
{
    public static TextField BindText(this TextField tf, string label, string value, Action<string> onChange)
    {
        tf.label = label; tf.value = value;
        tf.RegisterValueChangedCallback(e => onChange(e.newValue));
        return tf;
    }

    //public static EnumField BindEnum<T>(this EnumField ef, string label, T value, Action<T> onChange) where T : Enum
    //{
    //    ef.label = label; ef.value = value;
    //    ef.RegisterValueChangedCallback(e => onChange((T)e.newValue));
    //    return ef;
    //}

    public static Toggle BindToggle(this Toggle t, string label, bool value, Action<bool> onChange)
    {
        t.label = label; t.value = value;
        t.RegisterValueChangedCallback(e => onChange(e.newValue));
        return t;
    }

    //public static VisualElement Row(params VisualElement[] children)
    //{
    //    var row = new VisualElement();
    //    row.style.flexDirection = FlexDirection.Row;
    //    foreach (var c in children) row.Add(c);
    //    return row;
    //}
}
#endif
