#if UNITY_EDITOR
using System;
using UnityEngine.UIElements;

/// <summary>Drawer del editor para <see cref="EventDispatcherModule"/>.</summary>
public static class EventModuleDrawer
{
    public static VisualElement Draw(EventDispatcherModule m, Action onChanged)
    {
        var root = new VisualElement();
        ModuleDrawerStyles.ApplyRootPadding(root);

        var keyField = new TextField("Event Key") { value = m.eventKey };
        keyField.RegisterValueChangedCallback(e => { m.eventKey = e.newValue; onChanged?.Invoke(); });
        ModuleDrawerStyles.ApplyFieldMargins(keyField);
        root.Add(keyField);

        var typeField = new EnumField("Payload Type", m.payloadType);
        ModuleDrawerStyles.ApplyFieldMargins(typeField);
        var valueContainer = new VisualElement();

        void RebuildValueField()
        {
            valueContainer.Clear();
            switch (m.payloadType)
            {
                case EventPayloadType.Int:
                    var intField = new IntegerField("Value") { value = m.intValue };
                    intField.RegisterValueChangedCallback(e => { m.intValue = e.newValue; onChanged?.Invoke(); });
                    ModuleDrawerStyles.ApplyFieldMargins(intField);
                    valueContainer.Add(intField);
                    break;
                case EventPayloadType.Float:
                    var floatField = new FloatField("Value") { value = m.floatValue };
                    floatField.RegisterValueChangedCallback(e => { m.floatValue = e.newValue; onChanged?.Invoke(); });
                    ModuleDrawerStyles.ApplyFieldMargins(floatField);
                    valueContainer.Add(floatField);
                    break;
                case EventPayloadType.String:
                    var strField = new TextField("Value") { value = m.stringValue };
                    strField.RegisterValueChangedCallback(e => { m.stringValue = e.newValue; onChanged?.Invoke(); });
                    ModuleDrawerStyles.ApplyFieldMargins(strField);
                    valueContainer.Add(strField);
                    break;
                case EventPayloadType.Bool:
                    var boolField = new Toggle("Value") { value = m.boolValue };
                    boolField.RegisterValueChangedCallback(e => { m.boolValue = e.newValue; onChanged?.Invoke(); });
                    ModuleDrawerStyles.ApplyToggleMargins(boolField);
                    valueContainer.Add(boolField);
                    break;
                case EventPayloadType.Char:
                    var charField = new TextField("Value (1 char)") { value = m.stringValue, maxLength = 1 };
                    charField.RegisterValueChangedCallback(e => { m.stringValue = e.newValue; onChanged?.Invoke(); });
                    ModuleDrawerStyles.ApplyFieldMargins(charField);
                    valueContainer.Add(charField);
                    break;
            }
        }

        typeField.RegisterValueChangedCallback(e =>
        {
            m.payloadType = (EventPayloadType)e.newValue;
            RebuildValueField();
            onChanged?.Invoke();
        });

        root.Add(typeField);
        root.Add(valueContainer);
        RebuildValueField();

        ModuleDrawerStyles.AdjustFirstAndLastMargins(root);
        return root;
    }
}
#endif
