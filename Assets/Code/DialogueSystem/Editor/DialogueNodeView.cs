// Editor/GraphView/DialogueNodeView.cs
#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using EditorObjectField = UnityEditor.UIElements.ObjectField;

public class DialogueNodeView : Node
{
    public readonly DialogueNodeData Data;
    public readonly Vector2 MinSize = new(380, 160);
    public readonly Vector2 MaxSize = new(420, 560);
    public Vector2 DefaultSize => new(320, 200);

    private Port _input;
    private bool _didFirstAutosize = false;

    // --- NUEVO: referencias UI para alternar visibilidad ---
    private Toggle _locToggle;
    private TextField _textField;     // Texto literal
    private TextField _locKeyField;   // Clave de localización

    // --- TYPEWRITER UI ---
    private Toggle _twEnableToggle;
    private Toggle _twAdvancedToggle;
    private VisualElement _twAdvancedBox;

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

       
        var bg = Data.bgColor;
        mainContainer.style.backgroundColor = new StyleColor(bg);
        mainContainer.style.borderTopLeftRadius = 8;
        mainContainer.style.borderTopRightRadius = 8;
        mainContainer.style.borderBottomLeftRadius = 8;
        mainContainer.style.borderBottomRightRadius = 8;

        var border = new Color(0, 0, 0, 0.35f);
        mainContainer.style.borderLeftWidth = 1;
        mainContainer.style.borderRightWidth = 1;
        mainContainer.style.borderTopWidth = 1;
        mainContainer.style.borderBottomWidth = 1;
        mainContainer.style.borderLeftColor = border;
        mainContainer.style.borderRightColor = border;
        mainContainer.style.borderTopColor = border;
        mainContainer.style.borderBottomColor = border;

        // === Selector fijo (dropdown) con la paleta de DialogueNodeData ===
        var paletteNames = DialogueNodeData.NodePalette.Select(p => p.name).ToList();
        int safeIndex = Mathf.Clamp(Data.bgColorIndex, 0, paletteNames.Count - 1);

        // Sincroniza por si el asset viene con índice fuera de rango
        Data.bgColorIndex = safeIndex;
        Data.bgColor = DialogueNodeData.NodePalette[safeIndex].color;
        mainContainer.style.backgroundColor = new StyleColor(Data.bgColor);

        var paletteDropdown = new DropdownField("Color de fondo", paletteNames, safeIndex);
        paletteDropdown.RegisterValueChangedCallback(e =>
        {
            int idx = paletteNames.IndexOf(e.newValue);
            if (idx < 0) return;

            Data.bgColorIndex = idx;
            Data.bgColor = DialogueNodeData.NodePalette[idx].color;
            mainContainer.style.backgroundColor = new StyleColor(Data.bgColor);

            // Opcional: persistir al instante
            // UnityEditor.EditorUtility.SetDirty(/* tu DialogueGraph SO */);
            // AssetDatabase.SaveAssets();
        });

        mainContainer.Add(paletteDropdown);

        //// Espaciado agradable
        //mainContainer.style.paddingLeft = 8;
        //mainContainer.style.paddingRight = 8;
        //mainContainer.style.paddingTop = 6;
        //mainContainer.style.paddingBottom = 8;

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

        // ---------- NUEVO: TOGGLE Localización sobre el bloque de texto ----------
        _locToggle = new Toggle("Localización") { tooltip = "Activa para usar una clave de localización en vez de texto literal." };
        _locToggle.value = Data.localization;
        _locToggle.RegisterValueChangedCallback(e =>
        {
            Data.localization = e.newValue;
            UpdateLocalizationVisibility();
            ScheduleAutoSize();
        });
        mainContainer.Add(_locToggle);

        // ---------- Campo de texto literal ----------
        _textField = new TextField("Texto") { multiline = true, value = Data.lineText };
        _textField.RegisterValueChangedCallback(e => Data.lineText = e.newValue);
        mainContainer.Add(_textField);

        // ---------- Campo de clave de localización ----------
        _locKeyField = new TextField("Clave de localización") { value = Data.locKey };
        _locKeyField.RegisterValueChangedCallback(e => Data.locKey = e.newValue);
        mainContainer.Add(_locKeyField);

        UpdateLocalizationVisibility();

        // TYPEWRITER OPTIONS (placeholder, puedes expandir)

        // ---------- TYPEWRITER ----------
        _twEnableToggle = new Toggle("Typewriter") { tooltip = "Activar escritura progresiva en este nodo." };
        _twEnableToggle.value = Data.useTypewriter;
        _twEnableToggle.RegisterValueChangedCallback(e =>
        {
            Data.useTypewriter = e.newValue;
            UpdateTypewriterVisibility();
            ScheduleAutoSize();
        });
        mainContainer.Add(_twEnableToggle);

        // Toggle para mostrar/ocultar el bloque avanzado
        _twAdvancedToggle = new Toggle("Mostrar opciones avanzadas")
        {
            tooltip = "Muestra algunos parámetros comunes del Typewriter para ajustar en este nodo."
        };
        _twAdvancedToggle.value = Data.twShowAdvanced;
        _twAdvancedToggle.RegisterValueChangedCallback(e =>
        {
            Data.twShowAdvanced = e.newValue;
            UpdateTypewriterVisibility();
            ScheduleAutoSize();
        });
        mainContainer.Add(_twAdvancedToggle);

        // Contenedor avanzado (se oculta si no se usa)
        _twAdvancedBox = new VisualElement();
        _twAdvancedBox.style.marginLeft = 10;
        _twAdvancedBox.style.marginTop = 4;
        _twAdvancedBox.style.marginBottom = 4;
        _twAdvancedBox.style.flexDirection = FlexDirection.Column;

        // Campos mínimos y útiles
        var fSeconds = new FloatField("Segundos/char") { value = Mathf.Clamp(Data.tw.secondsPerChar, 0.001f, 0.2f) };
        fSeconds.RegisterValueChangedCallback(e =>
        {
            Data.tw.secondsPerChar = Mathf.Clamp(e.newValue, 0.001f, 0.2f);
        });

        var fGlobal = new FloatField("Velocidad global (x)") { value = Mathf.Clamp(Data.tw.globalSpeed, 0.1f, 3f) };
        fGlobal.RegisterValueChangedCallback(e =>
        {
            Data.tw.globalSpeed = Mathf.Clamp(e.newValue, 0.1f, 3f);
        });

        var tRich = new Toggle("Respetar RichText") { value = Data.tw.respectRichText };
        tRich.RegisterValueChangedCallback(e => Data.tw.respectRichText = e.newValue);

        var tWhitespace = new Toggle("Min. delay en espacios") { value = Data.tw.minimalWhitespaceDelay };
        tWhitespace.RegisterValueChangedCallback(e => Data.tw.minimalWhitespaceDelay = e.newValue);

        // Pausas comunes
        var fComma = new FloatField("Pausa coma (x)") { value = Data.tw.commaPct };
        fComma.RegisterValueChangedCallback(e => Data.tw.commaPct = Mathf.Max(0f, e.newValue));

        var fPeriod = new FloatField("Pausa punto (x)") { value = Data.tw.periodPct };
        fPeriod.RegisterValueChangedCallback(e => Data.tw.periodPct = Mathf.Max(0f, e.newValue));

        var fEllipsis = new FloatField("Pausa '...' (x)") { value = Data.tw.ellipsisPct };
        fEllipsis.RegisterValueChangedCallback(e => Data.tw.ellipsisPct = Mathf.Max(0f, e.newValue));

        _twAdvancedBox.Add(fSeconds);
        _twAdvancedBox.Add(fGlobal);
        _twAdvancedBox.Add(tRich);
        _twAdvancedBox.Add(tWhitespace);
        _twAdvancedBox.Add(fComma);
        _twAdvancedBox.Add(fPeriod);
        _twAdvancedBox.Add(fEllipsis);

        mainContainer.Add(_twAdvancedBox);

        // Ajustar visibilidad inicial
        UpdateTypewriterVisibility();


        // END TYPEWRITER OPTIONS

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

    private void UpdateTypewriterVisibility()
    {
        // Si no está activo el typewriter, ocultamos todo lo relacionado
        _twAdvancedToggle.style.display = Data.useTypewriter ? DisplayStyle.Flex : DisplayStyle.None;

        // Si está activo y además pedimos opciones avanzadas, mostramos el bloque
        bool showAdvanced = Data.useTypewriter && Data.twShowAdvanced;
        _twAdvancedBox.style.display = showAdvanced ? DisplayStyle.Flex : DisplayStyle.None;
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

    private void UpdateLocalizationVisibility()
    {
        // Si 'localizacion' está activa -> mostrar clave, ocultar texto literal
        if (_textField != null)
            _textField.style.display = Data.localization ? DisplayStyle.None : DisplayStyle.Flex;

        if (_locKeyField != null)
            _locKeyField.style.display = Data.localization ? DisplayStyle.Flex : DisplayStyle.None;
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
