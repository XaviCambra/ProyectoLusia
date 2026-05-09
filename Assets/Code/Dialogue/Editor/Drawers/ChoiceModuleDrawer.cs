#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

/// <summary>Drawer del editor para <see cref="ChoiceModule"/>.</summary>
public static class ChoiceModuleDrawer
{
    public static VisualElement Draw(ChoiceModule m, Action onChanged)
    {
        var root = new VisualElement();
        ModuleDrawerStyles.ApplyRootPadding(root);

        // Show blocked choices toggle
        var showBlockedToggle = new Toggle("Show Blocked Choices") { value = m.showBlockedChoices };
        showBlockedToggle.RegisterValueChangedCallback(e => { m.showBlockedChoices = e.newValue; onChanged?.Invoke(); });
        ModuleDrawerStyles.ApplyToggleMargins(showBlockedToggle);
        root.Add(showBlockedToggle);

        // Lista de choices
        var choiceList = new VisualElement();
        root.Add(choiceList);

        void RebuildChoiceList()
        {
            choiceList.Clear();
            for (int i = 0; i < m.choices.Count; i++)
            {
                var choice = m.choices[i];
                int capturedIndex = i;
                choiceList.Add(BuildChoiceItem(m, choice, capturedIndex, RebuildChoiceList, onChanged));
            }
        }

        // Botón para añadir choice
        var addBtn = new Button(() =>
        {
            m.choices.Add(new ChoiceModule.ChoiceData
            {
                choiceText = $"Option {m.choices.Count + 1}",
                portName   = System.Guid.NewGuid().ToString("N")[..8]
            });
            RebuildChoiceList();
            onChanged?.Invoke();
        }) { text = "+ Add Choice" };
        addBtn.style.marginTop = 4;
        root.Add(addBtn);

        RebuildChoiceList();

        ModuleDrawerStyles.AdjustFirstAndLastMargins(root);
        return root;
    }

    private static VisualElement BuildChoiceItem(
        ChoiceModule module,
        ChoiceModule.ChoiceData choice,
        int index,
        Action rebuildList,
        Action onChanged)
    {
        var item = new VisualElement();
        item.style.borderBottomWidth = 1;
        item.style.borderBottomColor = new UnityEngine.Color(0.3f, 0.3f, 0.3f, 1f);
        item.style.marginBottom = 4;

        // Header: label + delete button
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;

        var label = new Label($"Choice {index + 1}");
        label.style.flexGrow = 1;
        label.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;

        var deleteBtn = new Button(() =>
        {
            module.choices.RemoveAt(index);
            rebuildList();
            onChanged?.Invoke();
        }) { text = "✕" };
        deleteBtn.style.width = 24;

        header.Add(label);
        header.Add(deleteBtn);
        item.Add(header);

        // Port name (readonly display)
        var portLabel = new Label($"Port: {choice.portName}");
        portLabel.style.color = new UnityEngine.Color(0.6f, 0.6f, 0.6f, 1f);
        portLabel.style.fontSize = 10;
        item.Add(portLabel);

        // Choice text / localization
        var locToggle = new Toggle("Use Localization") { value = choice.choiceUseLocalization };
        ModuleDrawerStyles.ApplyToggleMargins(locToggle);
        var textField = new TextField("Text") { value = choice.choiceText };
        ModuleDrawerStyles.ApplyFieldMargins(textField);
        var locField  = new TextField("Loc Key") { value = choice.choiceLocKey };
        ModuleDrawerStyles.ApplyFieldMargins(locField);

        void UpdateLocVisibility()
        {
            textField.style.display = choice.choiceUseLocalization ? DisplayStyle.None : DisplayStyle.Flex;
            locField.style.display  = choice.choiceUseLocalization ? DisplayStyle.Flex  : DisplayStyle.None;
        }

        locToggle.RegisterValueChangedCallback(e => { choice.choiceUseLocalization = e.newValue; UpdateLocVisibility(); onChanged?.Invoke(); });
        textField.RegisterValueChangedCallback(e => { choice.choiceText = e.newValue; onChanged?.Invoke(); });
        locField.RegisterValueChangedCallback(e => { choice.choiceLocKey = e.newValue; onChanged?.Invoke(); });

        item.Add(locToggle);
        item.Add(textField);
        item.Add(locField);
        UpdateLocVisibility();

        // Affinity condition (foldout)
        item.Add(BuildAffinityFoldout(choice, onChanged));

        // Progress condition (foldout)
        item.Add(BuildProgressFoldout(choice, onChanged));

        return item;
    }

    private static Foldout BuildAffinityFoldout(ChoiceModule.ChoiceData c, Action onChanged)
    {
        var foldout = new Foldout { text = "Affinity Condition", value = false };

        var toggle = new Toggle("Requires Affinity") { value = c.requiresAffinity };
        ModuleDrawerStyles.ApplyToggleMargins(toggle);
        var fields = new VisualElement();
        fields.style.display = c.requiresAffinity ? DisplayStyle.Flex : DisplayStyle.None;

        toggle.RegisterValueChangedCallback(e =>
        {
            c.requiresAffinity = e.newValue;
            fields.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            onChanged?.Invoke();
        });

        var fromField = new ObjectField("From") { objectType = typeof(CharacterDefinition), value = c.affinityFrom };
        ModuleDrawerStyles.ApplyFieldMargins(fromField);

        var toField = new ObjectField("To") { objectType = typeof(CharacterDefinition), value = c.affinityTo };
        ModuleDrawerStyles.ApplyFieldMargins(toField);

        var bandContainer = new VisualElement();

        void RebuildBandDropdown()
        {
            bandContainer.Clear();
            var relationshipNames = GetRelationshipNames(c.affinityFrom, c.affinityTo);

            if (relationshipNames == null)
            {
                var warning = new HelpBox(
                    c.affinityFrom == null || c.affinityTo == null
                        ? "Asigna From y To."
                        : $"{c.affinityFrom.displayName} → {c.affinityTo.displayName} no tiene relación en el mapa.",
                    HelpBoxMessageType.Warning);
                bandContainer.Add(warning);
                return;
            }

            int idx = relationshipNames.IndexOf(c.requiredAffinityRelationship);
            if (idx < 0) { idx = 0; c.requiredAffinityRelationship = relationshipNames[0]; }
            var dropdown = new DropdownField("Required Relationship", relationshipNames, idx);
            ModuleDrawerStyles.ApplyFieldMargins(dropdown);
            dropdown.RegisterValueChangedCallback(e => { c.requiredAffinityRelationship = e.newValue; onChanged?.Invoke(); });
            bandContainer.Add(dropdown);
        }

        fromField.RegisterValueChangedCallback(e =>
        {
            c.affinityFrom = e.newValue as CharacterDefinition;
            RebuildBandDropdown();
            onChanged?.Invoke();
        });

        toField.RegisterValueChangedCallback(e =>
        {
            c.affinityTo = e.newValue as CharacterDefinition;
            RebuildBandDropdown();
            onChanged?.Invoke();
        });

        RebuildBandDropdown();

        var invToggle = new Toggle("Invert") { value = c.invertRequirement };
        invToggle.RegisterValueChangedCallback(e => { c.invertRequirement = e.newValue; onChanged?.Invoke(); });
        ModuleDrawerStyles.ApplyToggleMargins(invToggle);

        fields.Add(fromField);
        fields.Add(toField);
        fields.Add(bandContainer);
        fields.Add(invToggle);
        foldout.Add(toggle);
        foldout.Add(fields);
        return foldout;
    }

    // Devuelve null si el par no tiene relación registrada en el mapa.
    private static List<string> GetRelationshipNames(CharacterDefinition from, CharacterDefinition to)
    {
        if (from == null || to == null) return null;

        var schemaGuids = AssetDatabase.FindAssets("t:AffinitySchema");
        if (schemaGuids.Length == 0) return null;

        var schema  = AssetDatabase.LoadAssetAtPath<AffinitySchema>(AssetDatabase.GUIDToAssetPath(schemaGuids[0]));
        var mapGuids = AssetDatabase.FindAssets("t:CharacterAffinityMap");
        if (mapGuids.Length == 0) return null;

        var map   = AssetDatabase.LoadAssetAtPath<CharacterAffinityMap>(AssetDatabase.GUIDToAssetPath(mapGuids[0]));
        var entry = map.InitialEntries.FirstOrDefault(e => e.from == from && e.to == to);
        if (entry == null) return null;

        return schema.GetTrack(entry.trackId)?.Relationships.Select(b => b.name).ToList();
    }

    private static Foldout BuildProgressFoldout(ChoiceModule.ChoiceData c, Action onChanged)
    {
        var foldout = new Foldout { text = "Progress Condition", value = false };

        var toggle = new Toggle("Requires Progress") { value = c.requiresProgress };
        ModuleDrawerStyles.ApplyToggleMargins(toggle);
        var fields = new VisualElement();
        fields.style.display = c.requiresProgress ? DisplayStyle.Flex : DisplayStyle.None;

        toggle.RegisterValueChangedCallback(e =>
        {
            c.requiresProgress = e.newValue;
            fields.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            onChanged?.Invoke();
        });

        var methodField  = new TextField("Method Name") { value = c.progressMethod };
        methodField.RegisterValueChangedCallback(e => { c.progressMethod = e.newValue; onChanged?.Invoke(); });
        ModuleDrawerStyles.ApplyFieldMargins(methodField);

        var argTypeField = new EnumField("Arg Type", c.progressArgType);
        ModuleDrawerStyles.ApplyFieldMargins(argTypeField);

        var argContainer = new VisualElement();

        void RebuildArgField()
        {
            argContainer.Clear();
            switch (c.progressArgType)
            {
                case ProgressArgType.Int:
                    var intF = new IntegerField("Arg") { value = c.progressArgInt };
                    intF.RegisterValueChangedCallback(e => { c.progressArgInt = e.newValue; onChanged?.Invoke(); });
                    ModuleDrawerStyles.ApplyFieldMargins(intF);
                    argContainer.Add(intF);
                    break;
                case ProgressArgType.Float:
                    var floatF = new FloatField("Arg") { value = c.progressArgFloat };
                    floatF.RegisterValueChangedCallback(e => { c.progressArgFloat = e.newValue; onChanged?.Invoke(); });
                    ModuleDrawerStyles.ApplyFieldMargins(floatF);
                    argContainer.Add(floatF);
                    break;
                case ProgressArgType.String:
                    var strF = new TextField("Arg") { value = c.progressArgString };
                    strF.RegisterValueChangedCallback(e => { c.progressArgString = e.newValue; onChanged?.Invoke(); });
                    ModuleDrawerStyles.ApplyFieldMargins(strF);
                    argContainer.Add(strF);
                    break;
            }
        }

        argTypeField.RegisterValueChangedCallback(e =>
        {
            c.progressArgType = (ProgressArgType)e.newValue;
            RebuildArgField();
            onChanged?.Invoke();
        });

        fields.Add(methodField);
        fields.Add(argTypeField);
        fields.Add(argContainer);
        RebuildArgField();

        foldout.Add(toggle);
        foldout.Add(fields);
        return foldout;
    }
}
#endif
