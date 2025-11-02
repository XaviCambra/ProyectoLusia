// Editor/GraphView/DialogueNodeView.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using EditorObjectField = UnityEditor.UIElements.ObjectField;

public class DialogueNodeView : Node
{
    // ------------------------------
    // Public / Data
    // ------------------------------
    public readonly DialogueNodeData Data;
    public readonly Vector2 MinSize = new(480, 144);
    public readonly Vector2 MaxSize = new(480, 1080);
    public Vector2 DefaultSize => new(320, 200);

    // ------------------------------
    // Private UI refs
    // ------------------------------
    private Port _input;
    private bool _didFirstAutosize;

    // Localization UI
    private Toggle _locToggle;
    private TextField _textField;       // Texto literal
    private TextField _locKeyField;     // Clave de localización

    // Typewriter UI
    private Toggle _twEnableToggle;
    private FloatField _twSecondsPerCharField;
    private FloatField _twGlobalSpeedField;
    private Toggle _twRespectRichTextToggle;
    private Toggle _twWhitespaceDelayToggle;
    private FloatField _twCommaPauseField;
    private FloatField _twPeriodPauseField;
    private FloatField _twEllipsisPauseField;

    // Special Animation UI
    private Toggle _specialAnimToggle;
    private ObjectField _specialAnimClipField;
    private FloatField _specialAnimSpeedField;
    private Toggle _specialAnimLoopToggle;

    // Events (payload dinámico)
    private VisualElement _payloadValueContainer;

    // Start node field (visible-condicional)
    private TextField _startIdField;

    // Fábrica para payloads de evento
    private Dictionary<EventPayloadType, Func<VisualElement>> _eventFieldFactory;

    // ------------------------------
    // Ctor
    // ------------------------------
    public DialogueNodeView(DialogueNodeData data)
    {
        Data = data;

        ConfigureNodeChrome();
        BuildHeaderButtons();

        // --- NUEVO ORDEN / ESTRUCTURA ---
        BuildBasicHeader();           // Color fondo + Perfil + Retrato
        AddDivider("Texto y localización");
        BuildMainContentBlock();      // Nombre + Localización + Texto/LocKey

        AddDivider("Apariencia y animación");
        BuildAppearanceAndAnimation(); // Aparición / Move / Fade / Animación especial

        AddDivider("Typewriter");
        BuildTypewriterFoldout();     // (foldout)

        AddDivider("Flujo del diálogo");
        BuildStartAndChoiceBlock();   // Start + Choice

        AddDivider("Eventos");
        BuildEventsBlock();           // Event key + payload + probar

        BuildInputPort();             // Puertos
        RebuildOutputs();

        RegisterCallback<GeometryChangedEvent>(OnGeometryChangedOnce);
        SetPosition(Data.nodeRect);
        tooltip = $"GUID: {Data.GUID}";
    }

    // ------------------------------
    // Chrome / Layout
    // ------------------------------
    private void ConfigureNodeChrome()
    {
        title = "Diálogo";
        titleContainer.Q("collapse-button")?.RemoveFromHierarchy();
        viewDataKey = Data.GUID;

        // Caja principal
        mainContainer.style.paddingLeft = 6;
        mainContainer.style.paddingRight = 6;
        mainContainer.style.paddingBottom = 6;
        mainContainer.style.overflow = Overflow.Visible;

        style.flexDirection = FlexDirection.Column;
        style.minWidth = MinSize.x;
        style.minHeight = MinSize.y;
        style.maxWidth = MaxSize.x;
        //style.maxHeight = MaxSize.y;

        // Fondo y borde
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

        // Clases para estilado por USS (opcional)
        mainContainer.AddToClassList("dlg-node");
    }

    private void BuildHeaderButtons()
    {
        var delBtn = new Button(DeleteSelf) { text = "Eliminar nodo" };
        titleButtonContainer.Add(delBtn);
    }

    // ---------- Helpers visuales ----------
    private void AddDivider(string title = null)
    {
        if (!string.IsNullOrEmpty(title))
        {
            var label = new Label(title);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 6;
            label.style.marginBottom = 4;
            mainContainer.Add(label);
        }

        var line = new VisualElement();
        line.style.height = 1;
        line.style.marginBottom = 6;
        line.style.backgroundColor = new Color(0, 0, 0, 0.10f);
        mainContainer.Add(line);
    }

    // Línea fina reutilizable para cualquier contenedor (p.ej., outputContainer)
    private static void AddThinDividerTo(VisualElement container, float alpha = 0.10f)
    {
        var line = new VisualElement();
        line.style.height = 1;
        line.style.marginTop = 4;
        line.style.marginBottom = 4;
        line.style.backgroundColor = new Color(0, 0, 0, alpha);
        line.style.flexGrow = 1;
        container.Add(line);
    }

    // ------------------------------
    // Header básico: paleta, perfil, retrato
    // ------------------------------
    private void BuildBasicHeader()
    {
        BuildPalettePicker();
        BuildProfileSection();
        BuildPortraitKey();
    }

    private void BuildPalettePicker()
    {
        var paletteNames = DialogueNodeData.NodePalette.Select(p => p.name).ToList();
        int safeIndex = Mathf.Clamp(Data.bgColorIndex, 0, Mathf.Max(0, paletteNames.Count - 1));

        Data.bgColorIndex = safeIndex;
        if (paletteNames.Count > 0)
        {
            Data.bgColor = DialogueNodeData.NodePalette[safeIndex].color;
            mainContainer.style.backgroundColor = new StyleColor(Data.bgColor);
        }

        var paletteDropdown = new DropdownField("Color de fondo", paletteNames, safeIndex);
        paletteDropdown.RegisterValueChangedCallback(e =>
        {
            int idx = paletteNames.IndexOf(e.newValue);
            if (idx < 0) return;

            Data.bgColorIndex = idx;
            Data.bgColor = DialogueNodeData.NodePalette[idx].color;
            mainContainer.style.backgroundColor = new StyleColor(Data.bgColor);
            ScheduleAutoSize();
        });

        mainContainer.Add(paletteDropdown);
    }

    private void BuildProfileSection()
    {
        var profileField = new EditorObjectField("Perfil (SO)")
        {
            objectType = typeof(CharacterProfile),
            allowSceneObjects = false
        };

        // Prioriza referencia directa; si no existe, intenta localizar por profileId
        profileField.value = Data.profileRef != null ? Data.profileRef : FindProfileById(Data.profileId);

        profileField.RegisterValueChangedCallback(e =>
        {
            var so = e.newValue as CharacterProfile;

#if UNITY_EDITOR
            // Garantiza que el SO tiene un ID persistente
            if (so != null && string.IsNullOrEmpty(so.ProfileId))
            {
                var soObj = new SerializedObject(so);
                var idProp = soObj.FindProperty("profileId");
                idProp.stringValue = Guid.NewGuid().ToString();
                soObj.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(so);
                AssetDatabase.SaveAssets();
            }
#endif
            Data.profileRef = so;
            Data.profileId = so ? so.ProfileId : null;
            DGLog.Info($"NodeView('{Data.GUID}') perfil cambiado → ref='{(so ? so.name : "NULL")}' id='{Data.profileId}'");
        });

#if UNITY_EDITOR
        // Rehidratación del ObjectField en editor si solo tenemos el ID
        if (Data.profileRef == null && !string.IsNullOrEmpty(Data.profileId))
        {
            DGLog.Info($"NodeView('{Data.GUID}') intenta rehidratar profileRef desde profileId='{Data.profileId}'");
            var guids = AssetDatabase.FindAssets("t:CharacterProfile");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var so = AssetDatabase.LoadAssetAtPath<CharacterProfile>(path);
                if (so != null && so.ProfileId == Data.profileId)
                {
                    Data.profileRef = so;
                    DGLog.Info($"NodeView('{Data.GUID}') rehidratado: {so.name} ({so.ProfileId})");
                    break;
                }
            }
        }
        profileField.value = Data.profileRef;
#endif

        mainContainer.Add(profileField);
    }

    private CharacterProfile FindProfileById(string id)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(id)) return null;
        var guids = AssetDatabase.FindAssets("t:CharacterProfile");
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var so = AssetDatabase.LoadAssetAtPath<CharacterProfile>(path);
            if (so != null && so.ProfileId == id)
                return so;
        }
#endif
        return null;
    }

    private void BuildPortraitKey()
    {
        var portraitKeyField = new TextField("Retrato (key)") { value = Data.portraitKey };
        portraitKeyField.RegisterValueChangedCallback(e => Data.portraitKey = e.newValue);
        mainContainer.Add(portraitKeyField);
    }

    // ------------------------------
    // Contenido (Texto y Localización)
    // ------------------------------
    private void BuildMainContentBlock()
    {
        // Nombre del hablante
        mainContainer.Add(new TextField().BindText("Nombre", Data.speakerName, v => Data.speakerName = v));

        // Toggle Localización
        _locToggle = new Toggle("Localización")
        {
            tooltip = "Activa para usar una clave de localización en vez de texto literal.",
            value = Data.localization
        };
        _locToggle.RegisterValueChangedCallback(e =>
        {
            Data.localization = e.newValue;
            UpdateLocalizationVisibility();
            ScheduleAutoSize();
        });
        mainContainer.Add(_locToggle);

        // Texto literal
        _textField = new TextField("Texto") { multiline = true, value = Data.lineText };
        _textField.RegisterValueChangedCallback(e => Data.lineText = e.newValue);
        mainContainer.Add(_textField);

        // Clave de localización
        _locKeyField = new TextField("Clave de localización") { value = Data.locKey };
        _locKeyField.RegisterValueChangedCallback(e => Data.locKey = e.newValue);
        mainContainer.Add(_locKeyField);

        UpdateLocalizationVisibility();
    }

    private void UpdateLocalizationVisibility()
    {
        if (_textField != null)
            _textField.style.display = Data.localization ? DisplayStyle.None : DisplayStyle.Flex;

        if (_locKeyField != null)
            _locKeyField.style.display = Data.localization ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ------------------------------
    // Apariencia y Animación (foldout)
    // ------------------------------
    private void BuildAppearanceAndAnimation()
    {
        var extrasFold = new Foldout { text = "Apariencia y animación" };
        extrasFold.viewDataKey = Data.GUID + "_EXTRAS";
        extrasFold.value = Data.showExtrasBox;
        extrasFold.RegisterValueChangedCallback(e => { Data.showExtrasBox = e.newValue; });
        mainContainer.Add(extrasFold);

        // Aparición
        var appearanceField = new EnumField("Aparición", Data.appearance);
        appearanceField.Init(Data.appearance);
        appearanceField.RegisterValueChangedCallback(e => Data.appearance = (AppearanceMode)e.newValue);
        extrasFold.Add(appearanceField);

        // Origin (Spot) / Target (Spot)
        var originField = new EnumField("Origin", Data.origin);
        originField.Init(Data.origin);
        originField.RegisterValueChangedCallback(e => Data.origin = (Spot)e.newValue);
        extrasFold.Add(originField);

        var targetField = new EnumField("Target", Data.target);
        targetField.Init(Data.target);
        targetField.RegisterValueChangedCallback(e => Data.target = (Spot)e.newValue);
        extrasFold.Add(targetField);

        // Move speed
        var moveSpeedField = new FloatField("Move Speed (px/s)") { value = Data.moveSpeed };
        moveSpeedField.RegisterValueChangedCallback(e => Data.moveSpeed = Mathf.Max(0f, e.newValue));
        extrasFold.Add(moveSpeedField);

        // Inicio del texto
        var textStartField = new EnumField("Inicio del texto", Data.textStart);
        textStartField.Init(Data.textStart);
        textStartField.RegisterValueChangedCallback(e => Data.textStart = (TextStartTiming)e.newValue);
        extrasFold.Add(textStartField);

        // Fade + opacidades
        var useFadeToggle = new Toggle("Usar desvanecido") { value = Data.useFade };
        useFadeToggle.RegisterValueChangedCallback(e => Data.useFade = e.newValue);
        extrasFold.Add(useFadeToggle);

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

        // Mini separador y título “Animación especial”
        var saHeader = new Label("Animación especial");
        saHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        saHeader.style.marginTop = 6;
        extrasFold.Add(saHeader);
        var line = new VisualElement { style = { height = 1, backgroundColor = new Color(0, 0, 0, 0.08f), marginBottom = 6 } };
        extrasFold.Add(line);

        _specialAnimToggle = new Toggle("Usar animación especial") { value = Data.playSpecialAnimation };
        _specialAnimToggle.RegisterValueChangedCallback(e => Data.playSpecialAnimation = e.newValue);
        extrasFold.Add(_specialAnimToggle);

        _specialAnimClipField = new EditorObjectField
        {
            label = "Clip",
            objectType = typeof(AnimationClip),
            value = Data.specialAnimation
        };
        _specialAnimClipField.RegisterValueChangedCallback(e =>
        {
            Data.specialAnimation = e.newValue as AnimationClip;
        });
        extrasFold.Add(_specialAnimClipField);

        _specialAnimSpeedField = new FloatField("Velocidad") { value = Data.specialAnimSpeed };
        _specialAnimSpeedField.RegisterValueChangedCallback(e =>
        {
            Data.specialAnimSpeed = Mathf.Max(0f, e.newValue);
        });
        extrasFold.Add(_specialAnimSpeedField);

        _specialAnimLoopToggle = new Toggle("Loop") { value = Data.specialAnimLoop };
        _specialAnimLoopToggle.RegisterValueChangedCallback(e => Data.specialAnimLoop = e.newValue);
        extrasFold.Add(_specialAnimLoopToggle);
    }

    // ------------------------------
    // Typewriter (foldout)
    // ------------------------------
    private void BuildTypewriterFoldout()
    {
        var typewriterFold = new Foldout { text = "Typewriter" };
        typewriterFold.viewDataKey = Data.GUID + "_TW";
        typewriterFold.value = Data.showTypewriterBox;
        typewriterFold.RegisterValueChangedCallback(e => { Data.showTypewriterBox = e.newValue; });
        mainContainer.Add(typewriterFold);

        _twEnableToggle = new Toggle("Typewriter") { tooltip = "Activar escritura progresiva en este nodo.", value = Data.useTypewriter };
        _twEnableToggle.RegisterValueChangedCallback(e =>
        {
            Data.useTypewriter = e.newValue;
            ScheduleAutoSize();
        });
        typewriterFold.Add(_twEnableToggle);

        _twSecondsPerCharField = new FloatField("Segundos/char") { value = Mathf.Clamp(Data.tw.secondsPerChar, 0.001f, 0.2f) };
        _twSecondsPerCharField.RegisterValueChangedCallback(e => Data.tw.secondsPerChar = Mathf.Clamp(e.newValue, 0.001f, 0.2f));
        typewriterFold.Add(_twSecondsPerCharField);

        _twGlobalSpeedField = new FloatField("Velocidad global (x)") { value = Mathf.Clamp(Data.tw.globalSpeed, 0.1f, 3f) };
        _twGlobalSpeedField.RegisterValueChangedCallback(e => Data.tw.globalSpeed = Mathf.Clamp(e.newValue, 0.1f, 3f));
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

        _twEllipsisPauseField = new FloatField("Pausa '.' (x)") { value = Data.tw.ellipsisPct };
        _twEllipsisPauseField.RegisterValueChangedCallback(e => Data.tw.ellipsisPct = Mathf.Max(0f, e.newValue));
        typewriterFold.Add(_twEllipsisPauseField);
    }

    // ------------------------------
    // Start & Choice (flujo)
    // ------------------------------
    private void BuildStartAndChoiceBlock()
    {
        // Toggle: es nodo inicio
        var isStartToggle = new Toggle("Es nodo inicio") { value = Data.isStart };
        isStartToggle.RegisterValueChangedCallback(evt =>
        {
            Data.isStart = evt.newValue;
            RebuildOutputs();
            if (_startIdField != null)
                _startIdField.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        });
        mainContainer.Add(isStartToggle);

        // Start Id (visible solo si es nodo inicio)
        _startIdField = new TextField("Start Id")
        {
            value = Data.startId ?? string.Empty,
            style = { display = Data.isStart ? DisplayStyle.Flex : DisplayStyle.None }
        };
        _startIdField.RegisterValueChangedCallback(e => Data.startId = e.newValue);
        mainContainer.Add(_startIdField);

        // Nodo de elección
        mainContainer.Add(new Toggle().BindToggle("Es nodo de elección", Data.isChoiceNode, v =>
        {
            Data.isChoiceNode = v;
            RebuildOutputs();
        }));
    }

    // ------------------------------
    // Eventos
    // ------------------------------
    private void BuildEventsBlock()
    {
        // Event Key
        var eventKeyField = new TextField("Event Key") { value = Data.eventKey };
        eventKeyField.RegisterValueChangedCallback(evt =>
        {
            Data.eventKey = evt.newValue;
            ScheduleAutoSize();
        });
        mainContainer.Add(eventKeyField);

        // Selector de tipo
        var typeField = new EnumField("Payload Type", Data.eventPayloadType);
        typeField.Init(Data.eventPayloadType);
        typeField.RegisterValueChangedCallback(evt =>
        {
            Data.eventPayloadType = (EventPayloadType)evt.newValue;
            RebuildEventValueField();
            ScheduleAutoSize();
        });
        mainContainer.Add(typeField);

        // Contenedor dinámico
        _payloadValueContainer = new VisualElement { name = "payload-value-container" };
        mainContainer.Add(_payloadValueContainer);

        // Fábrica para el campo de valor
        InitEventFieldFactory();
        RebuildEventValueField();

        // Botón de prueba
        var testBtn = new Button(() =>
        {
            if (!string.IsNullOrEmpty(Data.eventKey))
            {
                var payload = Data.BuildEventPayload();
                GlobalDialogueEvents.Fire(payload);
                Debug.Log($"[DialogueNodeView] Probar evento -> {payload}");
            }
        })
        { text = "Probar evento" };
        mainContainer.Add(testBtn);
    }

    private void InitEventFieldFactory()
    {
        _eventFieldFactory = new Dictionary<EventPayloadType, Func<VisualElement>>
        {
            [EventPayloadType.None] = () => null,
            [EventPayloadType.Int] = () =>
            {
                var f = new IntegerField("Value (int)") { value = Data.eventInt };
                f.RegisterValueChangedCallback(e => Data.eventInt = e.newValue);
                return f;
            },
            [EventPayloadType.Float] = () =>
            {
                var f = new FloatField("Value (float)") { value = Data.eventFloat };
                f.RegisterValueChangedCallback(e => Data.eventFloat = e.newValue);
                return f;
            },
            [EventPayloadType.String] = () =>
            {
                var f = new TextField("Value (string)") { value = Data.eventString };
                f.RegisterValueChangedCallback(e => Data.eventString = e.newValue);
                return f;
            },
            [EventPayloadType.Bool] = () =>
            {
                var f = new Toggle("Value (bool)") { value = Data.eventBool };
                f.RegisterValueChangedCallback(e => Data.eventBool = e.newValue);
                return f;
            },
            [EventPayloadType.Char] = () =>
            {
                var initial = string.IsNullOrEmpty(Data.eventString) ? "" : Data.eventString.Substring(0, 1);
                var f = new TextField("Value (char)") { maxLength = 1, value = initial };
                f.RegisterValueChangedCallback(e =>
                {
                    Data.eventString = string.IsNullOrEmpty(e.newValue) ? "" : e.newValue.Substring(0, 1);
                });
                return f;
            },
        };
    }

    private void RebuildEventValueField()
    {
        _payloadValueContainer.Clear();
        if (_eventFieldFactory == null) InitEventFieldFactory();

        if (_eventFieldFactory.TryGetValue(Data.eventPayloadType, out var maker))
            _payloadValueContainer.Add(maker());

        ScheduleAutoSize();
    }

    // ------------------------------
    // Puertos
    // ------------------------------
    private void BuildInputPort()
    {
        _input = PortUtils.CreatePort(this, Direction.Input, Port.Capacity.Multi, "In");
        inputContainer.Add(_input);
    }

    private void ClearOutputs()
    {
        var children = new List<VisualElement>(outputContainer.Children());
        foreach (var c in children) outputContainer.Remove(c);
        outputContainer.Clear();
    }

    private void RebuildOutputs()
    {
        ClearOutputs();

        if (!Data.isChoiceNode)
        {
            // Nodo normal: un único "Next"
            var next = PortUtils.CreatePort(this, Direction.Output, Port.Capacity.Single, "Next");
            outputContainer.Add(next);
        }
        else
        {
            // Nodo de elección: contador + filas (TextField + Port)
            Data.choiceCount = Mathf.Clamp(Data.choiceCount, 2, 4);

            while (Data.choices.Count < Data.choiceCount)
                Data.choices.Add(new DialogueNodeData.ChoiceData
                {
                    choiceText = "Opción",
                    portName = Guid.NewGuid().ToString("N").Substring(0, 6)
                });

            while (Data.choices.Count > Data.choiceCount)
                Data.choices.RemoveAt(Data.choices.Count - 1);

            var countField = new IntegerField("Número de opciones") { value = Data.choiceCount };
            countField.RegisterValueChangedCallback(e =>
            {
                Data.choiceCount = Mathf.Clamp(e.newValue, 2, 4);
                RebuildOutputs();
            });
            outputContainer.Add(countField);

            for (int i = 0; i < Data.choiceCount; i++)
            {
                // --- Separador antes de cada opción (incluida la primera) ---
                AddThinDividerTo(outputContainer, 0.12f);

                var row = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center
                    }
                };

                var optField = new TextField($"Opción {i + 1}") { value = Data.choices[i].choiceText };
                optField.style.flexGrow = 1;
                int idx = i;
                optField.RegisterValueChangedCallback(e => Data.choices[idx].choiceText = e.newValue);

                var port = PortUtils.CreatePort(this, Direction.Output, Port.Capacity.Single, Data.choices[i].portName);
                port.tooltip = Data.choices[i].choiceText;

                row.Add(optField);
                row.Add(port);

                // --- UI de Afinidad por opción (apilado, vertical, sin 'gap') ---
                var affinityColumn = new VisualElement
                {
                    style =
    {
        flexDirection = FlexDirection.Column,
        marginTop = 2,
        marginBottom = 6
    }
                };

                // 1) Fila superior: SOLO el toggle "Req. afinidad"
                var headerRow = new VisualElement
                {
                    style =
    {
        flexDirection = FlexDirection.Row,
        alignItems = Align.Center
    }
                };
                var reqToggle = new Toggle("Req. afinidad")
                {
                    value = Data.choices[idx].requiresAffinity
                };
                reqToggle.style.minWidth = 0; // no fuerces ancho
                headerRow.Add(reqToggle);
                affinityColumn.Add(headerRow);

                // 2) Fila de parámetros: Clave + Valor + < que
                var paramsRow = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center
                    }
                };

                // Declaramos referencias ANTES de callbacks
                TextField affinityKeyField;
                FloatField affinityValueField;
                Toggle invertToggle;

                // Clave (string)
                affinityKeyField = new TextField("Clave")
                {
                    value = Data.choices[idx].affinityKey
                };
                // Compactar label y dar aire con marginRight
                affinityKeyField.labelElement.style.minWidth = 48;
                affinityKeyField.style.flexGrow = 1;
                affinityKeyField.style.marginRight = 6;
                paramsRow.Add(affinityKeyField);
                affinityKeyField.RegisterValueChangedCallback(e => Data.choices[idx].affinityKey = e.newValue);

                // Valor (float)
                affinityValueField = new FloatField("Valor")
                {
                    value = Data.choices[idx].requiredAffinity
                };
                affinityValueField.labelElement.style.minWidth = 44;
                affinityValueField.style.width = 110;
                affinityValueField.style.marginRight = 6;
                paramsRow.Add(affinityValueField);
                affinityValueField.RegisterValueChangedCallback(e => Data.choices[idx].requiredAffinity = e.newValue);

                // Invertir (< que)
                invertToggle = new Toggle("Bajo afinidad")
                {
                    value = Data.choices[idx].invertRequirement
                };
                paramsRow.Add(invertToggle);
                invertToggle.RegisterValueChangedCallback(e => Data.choices[idx].invertRequirement = e.newValue);

                // Helper: mostrar/ocultar la fila de parámetros
                void SetParamsVisible(bool on)
                {
                    paramsRow.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                }

                // Estado inicial
                SetParamsVisible(Data.choices[idx].requiresAffinity);

                // Callback del toggle principal
                reqToggle.RegisterValueChangedCallback(e =>
                {
                    Data.choices[idx].requiresAffinity = e.newValue;
                    SetParamsVisible(e.newValue);
                });

                // Montaje final
                affinityColumn.Add(paramsRow);
                outputContainer.Add(affinityColumn);

                outputContainer.Add(row);
            }
        }

        RefreshExpandedState();
        RefreshPorts();
        ScheduleAutoSize();
    }

    // ------------------------------
    // Autosize / Geometry
    // ------------------------------
    private void OnGeometryChangedOnce(GeometryChangedEvent evt)
    {
        if (_didFirstAutosize) return;
        _didFirstAutosize = true;

        // Primer ajuste al construirse
        ScheduleAutoSize();
    }

    private void ScheduleAutoSize()
    {
        // Un pequeño debounce visual para no recalcular varias veces en el mismo frame
        schedule.Execute(() =>
        {
            var r = GetPosition();

            // Altura: usa el contenido y añade margen
            float contentH = mainContainer.layout.height + inputContainer.layout.height + outputContainer.layout.height;
            float targetH = Mathf.Clamp(contentH + 20f, MinSize.y, MaxSize.y);

            // Anchura: calcula la mayor anchura de los contenedores (contentRect) + padding
            float contentW = Mathf.Max(
                mainContainer.contentRect.width,
                inputContainer.contentRect.width,
                outputContainer.contentRect.width
            );

            float padding = 24f;
            float targetW = Mathf.Clamp(Mathf.Max(r.width, contentW + padding, MinSize.x), MinSize.x, MaxSize.x);

            SetPosition(new Rect(r.x, r.y, targetW, targetH));
        }).ExecuteLater(0);
    }

    public override void SetPosition(Rect newPos)
    {
        newPos.width = Mathf.Clamp(newPos.width, MinSize.x, MaxSize.x);
        newPos.height = Mathf.Clamp(newPos.height, MinSize.y, MaxSize.y);
        base.SetPosition(newPos);
        Data.nodeRect = newPos;
    }

    private void DeleteSelf() => RemoveFromHierarchy();
}
#endif
