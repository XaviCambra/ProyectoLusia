// Editor/GraphView/DialogueNodeView.cs
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using EditorObjectField = UnityEditor.UIElements.ObjectField;


public class DialogueNodeView : Node
{
    public readonly DialogueNodeData Data;
    public readonly Vector2 MinSize = new(260, 140);
    public readonly Vector2 MaxSize = new(320, 420);
    public Vector2 DefaultSize => new(320, 200);

    private Port _input;
    private bool _didFirstAutosize = false;


    public DialogueNodeView(DialogueNodeData data)
    {
        Data = data;
        title = "Diologo";
        titleContainer.Q("collapse-button")?.RemoveFromHierarchy();
        viewDataKey = Data.GUID;

        mainContainer.style.paddingLeft = 6;
        mainContainer.style.paddingRight = 6;
        mainContainer.style.paddingBottom = 6;
        mainContainer.style.overflow = Overflow.Visible;

        style.flexDirection = FlexDirection.Column;
        style.minWidth = MinSize.x;
        style.minHeight = MinSize.y;
        style.maxWidth = MaxSize.x;
        style.maxHeight = MaxSize.y;

        // ---------- PERFIL (SO) ----------
        var profileField = new EditorObjectField("Perfil (SO)")
        {
            objectType = typeof(CharacterProfile),
            allowSceneObjects = false // ahora sí compila
        };
        profileField.value = FindProfileById(Data.profileId);
        profileField.RegisterValueChangedCallback(e =>
        {
            var so = e.newValue as CharacterProfile;
            Data.profileId = so ? so.ProfileId : null;
        });
        mainContainer.Add(profileField);

        // ---------- PORTRAIT KEY ----------
        var portraitKeyField = new TextField("Retrato (key)") { value = Data.portraitKey };
        portraitKeyField.RegisterValueChangedCallback(e => Data.portraitKey = e.newValue);
        mainContainer.Add(portraitKeyField);

        // ---------- Resto de tu UI ----------
        _input = PortUtils.CreatePort(this, Direction.Input, Port.Capacity.Multi, "In");
        inputContainer.Add(_input);

        mainContainer.Add(new TextField().BindText("Nombre", Data.speakerName, v => Data.speakerName = v));
        mainContainer.Add(new TextField().BindText("Texto", Data.lineText, v => Data.lineText = v));
        //mainContainer.Add(new EnumField().BindEnum("Posición", Data.anchor, (CharacterAnchor a) => Data.anchor = a));
        //var customPos = new Vector2Field("Custom") { value = Data.customAnchor };
        //customPos.RegisterValueChangedCallback(e => {
        //    var v = e.newValue;
        //    v.x = Mathf.Clamp01(v.x);
        //    v.y = Mathf.Clamp01(v.y);
        //    Data.customAnchor = v;
        //    customPos.SetValueWithoutNotify(v);
        //});
        mainContainer.Add(new Toggle().BindToggle("Es nodo inicial", Data.isStart, v => { Data.isStart = v; RebuildOutputs(); }));
        mainContainer.Add(new Toggle().BindToggle("Es nodo de elección", Data.isChoiceNode, v => { Data.isChoiceNode = v; RebuildOutputs(); }));
        mainContainer.Add(new TextField().BindText("Event Key", Data.eventKey, v => Data.eventKey = v));

        var testEventBtn = new Button(() =>
        {
            GlobalDialogueEvents.Fire(Data.eventKey);
            EditorUtility.DisplayDialog("Evento lanzado", $"Se disparo la clave: {Data.eventKey}", "OK");
        })
        { text = "Probar evento" };
        mainContainer.Add(testEventBtn);

        var delBtn = new Button(DeleteSelf) { text = "Eliminar nodo" };
        titleButtonContainer.Add(delBtn);

        RebuildOutputs();
        RegisterCallback<GeometryChangedEvent>(OnGeometryChangedOnce);
        SetPosition(Data.nodeRect);
        tooltip = $"GUID: {Data.GUID}";
    }

    // --- Helper para resolver el asset desde el ID guardado en el nodo ---
    private CharacterProfile FindProfileById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var guids = AssetDatabase.FindAssets("t:CharacterProfile");
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var p = AssetDatabase.LoadAssetAtPath<CharacterProfile>(path);
            if (p != null && p.ProfileId == id)
                return p;
        }
        return null;
    }

    private void OnGeometryChangedOnce(GeometryChangedEvent evt)
    {
        // Solo la primera vez que se genera el layout inicial
        if (_didFirstAutosize) return;
        _didFirstAutosize = true;
        ScheduleAutoSize();
    }

    private void ScheduleAutoSize()
    {
        // Espera un frame para que UI Toolkit tenga medidas correctas
        this.schedule.Execute(() => AutoSizeToContent()).ExecuteLater(0);
    }

    private void AutoSizeToContent()
    {
        // Calculamos tamaño preferido a partir del contenido real
        var r = GetPosition();

        // Altura: suma de contenedores internos ya medidos
        float inH = inputContainer.contentRect.height;
        float main = mainContainer.contentRect.height;
        float outH = outputContainer.contentRect.height;

        // Margen extra para borde/título
        float extra = 30f;

        float targetH = Mathf.Clamp(inH + main + outH + extra, MinSize.y, MaxSize.y);

        // Ancho: al menos el mínimo y lo necesario para el contenido
        float contentW = Mathf.Max(
            mainContainer.contentRect.width,
            inputContainer.contentRect.width,
            outputContainer.contentRect.width
        );

        float padding = 24f; // margen para scrollbars y bordes
        float targetW = Mathf.Clamp(Mathf.Max(r.width, contentW + padding, MinSize.x), MinSize.x, MaxSize.x);

        SetPosition(new Rect(r.x, r.y, targetW, targetH));
    }

    private void DeleteSelf() => RemoveFromHierarchy();

    private void ClearOutputs()
    {
        var children = new System.Collections.Generic.List<VisualElement>(outputContainer.Children());
        foreach (var c in children) outputContainer.Remove(c);
        outputContainer.Clear();
    }

    private void RebuildOutputs()
    {
        ClearOutputs();

        if (!Data.isChoiceNode)
        {
            var next = PortUtils.CreatePort(this, Direction.Output, Port.Capacity.Single, "Next");
            outputContainer.Add(next);
        }
        else
        {
            Data.choiceCount = Mathf.Clamp(Data.choiceCount, 2, 4);
            while (Data.choices.Count < Data.choiceCount)
                Data.choices.Add(new ChoiceData { choiceText = "Opcion", portName = Guid.NewGuid().ToString("N").Substring(0, 6) });
            while (Data.choices.Count > Data.choiceCount)
                Data.choices.RemoveAt(Data.choices.Count - 1);

            var countField = new IntegerField("No opciones") { value = Data.choiceCount };
            countField.RegisterValueChangedCallback(e => { Data.choiceCount = Mathf.Clamp(e.newValue, 2, 4); RebuildOutputs(); });
            outputContainer.Add(countField);

            for (int i = 0; i < Data.choiceCount; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                var optField = new TextField($"Opcion {i + 1}") { value = Data.choices[i].choiceText };
                optField.style.flexGrow = 1;
                int idx = i;
                optField.RegisterValueChangedCallback(e => Data.choices[idx].choiceText = e.newValue);

                var port = PortUtils.CreatePort(this, Direction.Output, Port.Capacity.Single, Data.choices[i].portName);
                port.tooltip = Data.choices[i].choiceText;

                row.Add(optField);
                row.Add(port);
                outputContainer.Add(row);
            }
        }

        RefreshExpandedState();
        RefreshPorts();
        ScheduleAutoSize();
    }

    public override void SetPosition(Rect newPos)
    {
        newPos.width = Mathf.Clamp(newPos.width, MinSize.x, MaxSize.x);
        newPos.height = Mathf.Clamp(newPos.height, MinSize.y, MaxSize.y);
        base.SetPosition(newPos);
        Data.nodeRect = newPos;
    }
}
#endif
