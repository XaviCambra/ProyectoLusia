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
    public readonly Vector2 MinSize = new(480, 144);
    public readonly Vector2 MaxSize = new(480, 1080);
    public Vector2 DefaultSize => new(320, 200);

    private Port _input;
    private bool _didFirstAutosize = false;

    // --- NUEVO: referencias UI para alternar visibilidad ---
    private Toggle _locToggle;
    private TextField _textField;     // Texto literal
    private TextField _locKeyField;   // Clave de localización

    // --- TYPEWRITER UI ---
    private Toggle _twEnableToggle;

    // Campos avanzados sin caja (sueltos dentro del foldout)
    private FloatField _twSecondsPerCharField;
    private FloatField _twGlobalSpeedField;
    private Toggle _twRespectRichTextToggle;
    private Toggle _twWhitespaceDelayToggle;
    private FloatField _twCommaPauseField;
    private FloatField _twPeriodPauseField;
    private FloatField _twEllipsisPauseField;

    private VisualElement _payloadValueContainer; // contenedor dinámico para el valor del evento

    public DialogueNodeView(DialogueNodeData data)
    {
        Data = data;
        title = "Diálogo";
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
        });

        mainContainer.Add(paletteDropdown);

        // ---------- PERFIL (SO) ----------
        var profileField = new EditorObjectField("Perfil (SO)")
        {
            objectType = typeof(CharacterProfile),
            allowSceneObjects = false // ahora sí compila
        };
        // Prioriza la referencia directa si existe; si no, intenta por ID.
        profileField.value = Data.profileRef != null ? Data.profileRef : FindProfileById(Data.profileId);
        profileField.RegisterValueChangedCallback(e =>
        {
            var so = e.newValue as CharacterProfile;

#if UNITY_EDITOR
            if (so != null && string.IsNullOrEmpty(so.ProfileId))
            {
                // Autogenera un ID persistente y lo guarda en el asset del perfil
                var soObj = new SerializedObject(so);
                var idProp = soObj.FindProperty("profileId"); // campo privado del SO
                idProp.stringValue = System.Guid.NewGuid().ToString();
                soObj.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(so);
                AssetDatabase.SaveAssets();
            }
#endif

            // Ahora sí: el nodo guarda un ID válido y persistente
            Data.profileRef = so;
            Data.profileId = so ? so.ProfileId : null;
            DGLog.Info($"NodeView('{Data.GUID}') perfil cambiado → ref='{(so ? so.name : "NULL")}' id='{Data.profileId}'");
        });

        mainContainer.Add(profileField);
        // ---------- END PERFIL (SO) ----------

#if UNITY_EDITOR
        // Si profileRef está vacío pero tenemos profileId, intenta rehidratarlo (editor) para que el ObjectField se muestre correcto
        if (Data.profileRef == null && !string.IsNullOrEmpty(Data.profileId))
        {
            var guid = Data.profileId;
            // Nota: profileId lo generas con Guid.NewGuid(), NO es el GUID del asset de Unity.
            // Para rehidratar por editor aquí, necesitas la base de datos:
            DGLog.Info($"NodeView('{Data.GUID}') intenta rehidratar profileRef desde profileId='{Data.profileId}'");
            var guids = AssetDatabase.FindAssets("t:CharacterProfile");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var so = AssetDatabase.LoadAssetAtPath<CharacterProfile>(path);
                if (so != null && so.ProfileId == guid)
                {
                    Data.profileRef = so;
                    DGLog.Info($"NodeView('{Data.GUID}') rehidratado: {so.name} ({so.ProfileId})");
                    break;
                }
            }
        }
        profileField.value = Data.profileRef;
#endif

        // ---------- PORTRAIT KEY ----------
        var portraitKeyField = new TextField("Retrato (key)") { value = Data.portraitKey };
        portraitKeyField.RegisterValueChangedCallback(e => Data.portraitKey = e.newValue);
        mainContainer.Add(portraitKeyField);

        // ---------- EXTRAS / ANIMACIÓN (PLEGABLE) ----------
        var extrasFold = new Foldout { text = "Extras / Animación" };
        extrasFold.viewDataKey = Data.GUID + "_EXTRAS";
        extrasFold.value = Data.showExtrasBox; // estado inicial
        extrasFold.RegisterValueChangedCallback(e => { Data.showExtrasBox = e.newValue; });
        mainContainer.Add(extrasFold);

        // Aparición
        var appearanceField = new EnumField("Aparición", Data.appearance);
        appearanceField.Init(Data.appearance);
        appearanceField.RegisterValueChangedCallback(e => Data.appearance = (AppearanceMode)e.newValue);
        extrasFold.Add(appearanceField);

        // Origin (Spot)
        var originField = new EnumField("Origin", Data.origin);
        originField.Init(Data.origin);
        originField.RegisterValueChangedCallback(e => Data.origin = (Spot)e.newValue);
        extrasFold.Add(originField);

        // Target (Spot)
        var targetField = new EnumField("Target", Data.target);
        targetField.Init(Data.target);
        targetField.RegisterValueChangedCallback(e => Data.target = (Spot)e.newValue);
        extrasFold.Add(targetField);

        // Move speed
        var moveSpeedField = new FloatField("Move Speed (px/s)") { value = Data.moveSpeed };
        moveSpeedField.RegisterValueChangedCallback(e => Data.moveSpeed = Mathf.Max(0f, e.newValue));
        extrasFold.Add(moveSpeedField);

        // Cuándo empieza el texto
        var textStartField = new EnumField("Inicio del texto", Data.textStart);
        textStartField.Init(Data.textStart);
        textStartField.RegisterValueChangedCallback(e => Data.textStart = (TextStartTiming)e.newValue);
        extrasFold.Add(textStartField);

        // Fade
        var useFadeToggle = new Toggle("Usar desvanecido") { value = Data.useFade };
        useFadeToggle.RegisterValueChangedCallback(e => Data.useFade = e.newValue);
        extrasFold.Add(useFadeToggle);

        // Opacidades (en %)
        var enterFrom = new IntegerField("Enter From %") { value = Data.enterFromOpacity };
        enterFrom.RegisterValueChangedCallback(e => Data.enterFromOpacity = Mathf.Clamp(e.newValue, 0, 100));
        extrasFold.Add(enterFrom);

        var enterTo = new IntegerField("Enter To %") { value = Data.enterToOpacity };
        enterTo.RegisterValueChangedCallback(e => Data.enterToOpacity = Mathf.Clamp(e.newValue, 0, 100));
        extrasFold.Add(enterTo);

        var exitFrom = new IntegerField("Exit From %") { value = Data.exitFromOpacity };
        exitFrom.RegisterValueChangedCallback(e => Data.exitFromOpacity = Mathf.Clamp(e.newValue, 0, 100));
        extrasFold.Add(exitFrom);

        var exitTo = new IntegerField("Exit To %") { value = Data.exitToOpacity };
        exitTo.RegisterValueChangedCallback(e => Data.exitToOpacity = Mathf.Clamp(e.newValue, 0, 100));
        extrasFold.Add(exitTo);
        // ---------- END EXTRAS / ANIMACIÓN (PLEGABLE) ----------

        

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

        // ---------- TYPEWRITER (PLEGABLE) ----------
        var typewriterFold = new Foldout { text = "Typewriter" };
        typewriterFold.viewDataKey = Data.GUID + "_TW";
        typewriterFold.value = Data.showTypewriterBox; // estado inicial
        typewriterFold.RegisterValueChangedCallback(e => { Data.showTypewriterBox = e.newValue; });
        mainContainer.Add(typewriterFold);

        // Toggle principal (usar typewriter)
        _twEnableToggle = new Toggle("Typewriter") { tooltip = "Activar escritura progresiva en este nodo." };
        _twEnableToggle.value = Data.useTypewriter;
        _twEnableToggle.RegisterValueChangedCallback(e =>
        {
            Data.useTypewriter = e.newValue;
            ScheduleAutoSize();
        });
        typewriterFold.Add(_twEnableToggle);

        // === CAMPOS AVANZADOS (sueltos, sin caja) ===
        _twSecondsPerCharField = new FloatField("Segundos/char") { value = Mathf.Clamp(Data.tw.secondsPerChar, 0.001f, 0.2f) };
        _twSecondsPerCharField.RegisterValueChangedCallback(e =>
        {
            Data.tw.secondsPerChar = Mathf.Clamp(e.newValue, 0.001f, 0.2f);
        });
        typewriterFold.Add(_twSecondsPerCharField);

        _twGlobalSpeedField = new FloatField("Velocidad global (x)") { value = Mathf.Clamp(Data.tw.globalSpeed, 0.1f, 3f) };
        _twGlobalSpeedField.RegisterValueChangedCallback(e =>
        {
            Data.tw.globalSpeed = Mathf.Clamp(e.newValue, 0.1f, 3f);
        });
        typewriterFold.Add(_twGlobalSpeedField);

        _twRespectRichTextToggle = new Toggle("Respetar RichText") { value = Data.tw.respectRichText };
        _twRespectRichTextToggle.RegisterValueChangedCallback(e => Data.tw.respectRichText = e.newValue);
        typewriterFold.Add(_twRespectRichTextToggle);

        _twWhitespaceDelayToggle = new Toggle("Min. delay en espacios") { value = Data.tw.minimalWhitespaceDelay };
        _twWhitespaceDelayToggle.RegisterValueChangedCallback(e => Data.tw.minimalWhitespaceDelay = e.newValue);
        typewriterFold.Add(_twWhitespaceDelayToggle);

        _twCommaPauseField = new FloatField("Pausa coma (x)") { value = Data.tw.commaPct };
        _twCommaPauseField.RegisterValueChangedCallback(e => Data.tw.commaPct = Mathf.Max(0f, e.newValue));
        typewriterFold.Add(_twCommaPauseField);

        _twPeriodPauseField = new FloatField("Pausa punto (x)") { value = Data.tw.periodPct };
        _twPeriodPauseField.RegisterValueChangedCallback(e => Data.tw.periodPct = Mathf.Max(0f, e.newValue));
        typewriterFold.Add(_twPeriodPauseField);

        _twEllipsisPauseField = new FloatField("Pausa '...' (x)") { value = Data.tw.ellipsisPct };
        _twEllipsisPauseField.RegisterValueChangedCallback(e => Data.tw.ellipsisPct = Mathf.Max(0f, e.newValue));
        typewriterFold.Add(_twEllipsisPauseField);
        // ---------- END TYPEWRITER (PLEGABLE) ----------

        mainContainer.Add(new Toggle().BindToggle("Es nodo inicial", Data.isStart, v => { Data.isStart = v; RebuildOutputs(); }));
        mainContainer.Add(new Toggle().BindToggle("Es nodo de elección", Data.isChoiceNode, v => { Data.isChoiceNode = v; RebuildOutputs(); }));
        //mainContainer.Add(new TextField().BindText("Event Key", Data.eventKey, v => Data.eventKey = v));

        // --- EVENTOS ---
        // Event Key
        var eventKeyField = new TextField("Event Key");
        eventKeyField.value = Data.eventKey;
        eventKeyField.RegisterValueChangedCallback(evt => {
            Data.eventKey = evt.newValue;
            ScheduleAutoSize(); // (opcional, para recalcular altura)
        });
        // contentContainer.Add(eventKeyField);
        mainContainer.Add(eventKeyField);

        // Selector de tipo
        var typeField = new EnumField("Payload Type", Data.eventPayloadType);
        typeField.Init(Data.eventPayloadType);
        typeField.RegisterValueChangedCallback(evt => {
            Data.eventPayloadType = (EventPayloadType)evt.newValue;
            RebuildEventValueField();
            ScheduleAutoSize(); // (opcional)
        });
        // contentContainer.Add(typeField);
        mainContainer.Add(typeField);

        // Contenedor dinámico para el valor
        _payloadValueContainer = new VisualElement { name = "payload-value-container" };
        mainContainer.Add(_payloadValueContainer);

        // Construye el campo según el tipo actual
        RebuildEventValueField();
        // --- END EVENTOS ---

        var testBtn = new Button(() =>
        {
            if (!string.IsNullOrEmpty(Data.eventKey))
            {
                var payload = Data.BuildEventPayload();
                GlobalDialogueEvents.Fire(payload);
                UnityEngine.Debug.Log($"[DialogueNodeView] Probar evento -> {payload}");
            }
        })
        { text = "Probar evento" };
        // contentContainer.Add(testBtn);
        mainContainer.Add(testBtn);

        var delBtn = new Button(DeleteSelf) { text = "Eliminar nodo" };
        titleButtonContainer.Add(delBtn);

        RebuildOutputs();
        RegisterCallback<GeometryChangedEvent>(OnGeometryChangedOnce);
        SetPosition(Data.nodeRect);
        tooltip = $"GUID: {Data.GUID}";
    }

    void RebuildEventValueField()
    {
        _payloadValueContainer.Clear();
        switch (Data.eventPayloadType)
        {
            case EventPayloadType.Int:
                var intField = new IntegerField("Value (int)") { value = Data.eventInt };
                intField.RegisterValueChangedCallback(e => { Data.eventInt = e.newValue; });
                _payloadValueContainer.Add(intField);
                break;

            case EventPayloadType.Float:
                var floatField = new FloatField("Value (float)") { value = Data.eventFloat };
                floatField.RegisterValueChangedCallback(e => { Data.eventFloat = e.newValue; });
                _payloadValueContainer.Add(floatField);
                break;

            case EventPayloadType.String:
                var strField = new TextField("Value (string)") { value = Data.eventString };
                strField.RegisterValueChangedCallback(e => { Data.eventString = e.newValue; });
                _payloadValueContainer.Add(strField);
                break;

            case EventPayloadType.Bool:
                var boolField = new Toggle("Value (bool)") { value = Data.eventBool };
                boolField.RegisterValueChangedCallback(e => { Data.eventBool = e.newValue; });
                _payloadValueContainer.Add(boolField);
                break;

            case EventPayloadType.Char:
                var charField = new TextField("Value (char)") { value = Data.eventString, maxLength = 1 };
                charField.RegisterValueChangedCallback(e => {
                    Data.eventString = string.IsNullOrEmpty(e.newValue) ? "" : e.newValue.Substring(0, 1);
                });
                _payloadValueContainer.Add(charField);
                break;

            case EventPayloadType.None:
            default:
                // No value UI
                break;
        }
        ScheduleAutoSize(); // recalcula tamaño tras reconstruir UI
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
                Data.choices.Add(new ChoiceData { choiceText = "Opción", portName = Guid.NewGuid().ToString("N").Substring(0, 6) });
            while (Data.choices.Count > Data.choiceCount)
                Data.choices.RemoveAt(Data.choices.Count - 1);

            var countField = new IntegerField("Número de opciones") { value = Data.choiceCount };
            countField.RegisterValueChangedCallback(e => { Data.choiceCount = Mathf.Clamp(e.newValue, 2, 4); RebuildOutputs(); });
            outputContainer.Add(countField);

            for (int i = 0; i < Data.choiceCount; i++)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                var optField = new TextField($"Opción {i + 1}") { value = Data.choices[i].choiceText };
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
