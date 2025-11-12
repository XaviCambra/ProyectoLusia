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

    // Notificación al exterior cuando cambian datos persistentes del nodo
    public Action OnDataChanged;

    // ------------------------------
    // Ctor
    // ------------------------------
    public DialogueNodeView(DialogueNodeData data)
    {
        Data = data;

        ConfigureNodeChrome();
        BuildHeaderButtons();

        Debug.LogWarning("RECUERDA ENCAPSULARLO EN UN MATODO Y HACERLO PARAMETRICO ARRIBA EN AJUSTES");
        titleContainer.style.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.25f);
        inputContainer.style.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.1f);
        outputContainer.style.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.15f);

        // --- NUEVO ORDEN / ESTRUCTURA ---
        AddDivider("Perfil y color de fondo");
        BuildProfileAndBackgroundFoldout();   // Foldout: Color fondo + Perfil + Retrato

        AddDivider("Texto y localización");
        BuildTextAndLocalizationFoldout();    // Foldout: Nombre + Localización + Texto/LocKey

        AddDivider("Apariencia y animación");
        BuildAppearanceAndAnimation(); // Aparición / Move / Fade / Animación especial

        AddDivider("Animación especial");
        BuildSpecialAnimationFoldout(); // Foldout independiente para la animación especial

        AddDivider("Typewriter");
        BuildTypewriterFoldout();     // (foldout)

        AddDivider("Flujo del diálogo");
        BuildDialogueFlowFoldout();           // Foldout: Start + Choice

        AddDivider("Eventos");
        BuildEventsFoldout();                 // Foldout: Event key + payload + probar

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
        mainContainer.style.borderTopLeftRadius = 4;
        mainContainer.style.borderTopRightRadius = 4;
        mainContainer.style.borderBottomLeftRadius = 4;
        mainContainer.style.borderBottomRightRadius = 4;

        var border = new Color(0, 0, 0, 0.25f);
        mainContainer.style.borderLeftWidth = 0;
        mainContainer.style.borderRightWidth = 0;
        mainContainer.style.borderTopWidth = 0;
        mainContainer.style.borderBottomWidth = 4;
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
            label.style.marginTop = 8;
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
    // Perfil y color de fondo (foldout independiente)
    // ------------------------------
    private void BuildProfileAndBackgroundFoldout()
    {
        var fold = new Foldout { text = "Perfil y color de fondo" };
        fold.viewDataKey = Data.GUID + "_PROFILE_BG";
        fold.style.unityFontStyleAndWeight = FontStyle.Italic;
        fold.style.marginBottom = 3;

        // --- Color de fondo (paleta) ---
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
            paletteDropdown.style.marginBottom = 3;
            paletteDropdown.RegisterValueChangedCallback(e =>
            {
                int idx = paletteNames.IndexOf(e.newValue);
                if (idx < 0) return;

                Data.bgColorIndex = idx;
                Data.bgColor = DialogueNodeData.NodePalette[idx].color;
                mainContainer.style.backgroundColor = new StyleColor(Data.bgColor);
                ScheduleAutoSize();
            });
            fold.Add(paletteDropdown);
        }

        // --- Perfil (SO) ---
        {
            var profileField = new EditorObjectField("Perfil (SO)")
            {
                objectType = typeof(CharacterProfile),
                allowSceneObjects = false
            };

            profileField.value = Data.profileRef != null ? Data.profileRef : FindProfileById(Data.profileId);
            profileField.style.marginBottom = 3;

            profileField.RegisterValueChangedCallback(e =>
            {
                var so = e.newValue as CharacterProfile;
                Data.profileRef = so;
                Data.profileId = so ? so.ProfileId : null;
                OnDataChanged?.Invoke();
            });

#if UNITY_EDITOR
            if (Data.profileRef == null && !string.IsNullOrEmpty(Data.profileId))
            {
                var guids = AssetDatabase.FindAssets("t:CharacterProfile");
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    var so = AssetDatabase.LoadAssetAtPath<CharacterProfile>(path);
                    if (so != null && so.ProfileId == Data.profileId)
                    {
                        Data.profileRef = so;
                        break;
                    }
                }
            }
            profileField.value = Data.profileRef;
#endif

            fold.Add(profileField);
        }

        // --- Retrato (key) ---
        {
            var portraitKeyField = new TextField("Retrato (key)") { value = Data.portraitKey };
            portraitKeyField.RegisterValueChangedCallback(e => Data.portraitKey = e.newValue);
            fold.Add(portraitKeyField);
        }

        mainContainer.Add(fold);
    }

    // ------------------------------
    // Texto y localización (foldout independiente)
    // ------------------------------
    private void BuildTextAndLocalizationFoldout()
    {
        var fold = new Foldout { text = "Texto y localización" };
        fold.viewDataKey = Data.GUID + "_TEXT_LOC";
        fold.style.unityFontStyleAndWeight = FontStyle.Italic;
        fold.style.marginBottom = 3;

        // Nombre del hablante
        fold.Add(new TextField().BindText("Nombre", Data.speakerName, v => Data.speakerName = v));

        // Toggle Localización
        _locToggle = new Toggle("Localización")
        {
            tooltip = "Activa para usar una clave de localización en vez de texto literal.",
            value = Data.localization
        };
        _locToggle.style.marginTop = 3;
        _locToggle.style.marginBottom = 3;
        _locToggle.RegisterValueChangedCallback(e =>
        {
            Data.localization = e.newValue;
            UpdateLocalizationVisibility();
            ScheduleAutoSize();
        });
        fold.Add(_locToggle);

        // Texto literal
        _textField = new TextField("Texto") { multiline = true, value = Data.lineText };
        _textField.RegisterValueChangedCallback(e => Data.lineText = e.newValue);
        fold.Add(_textField);

        // Clave de localización
        _locKeyField = new TextField("Clave de localización") { value = Data.locKey };
        _locKeyField.RegisterValueChangedCallback(e => Data.locKey = e.newValue);
        fold.Add(_locKeyField);

        UpdateLocalizationVisibility();
        mainContainer.Add(fold);
    }

    // ------------------------------
    // Flujo del diálogo (foldout independiente)
    // ------------------------------
    private void BuildDialogueFlowFoldout()
    {
        var fold = new Foldout { text = "Flujo del diálogo" };
        fold.viewDataKey = Data.GUID + "_FLOW";
        fold.style.unityFontStyleAndWeight = FontStyle.Italic;
        fold.style.marginBottom = 3;

        // Toggle: es nodo inicio
        var isStartToggle = new Toggle("Es nodo inicio") { value = Data.isStart };
        isStartToggle.RegisterValueChangedCallback(evt =>
        {
            Data.isStart = evt.newValue;
            RebuildOutputs();
            if (_startIdField != null)
                _startIdField.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        });
        fold.Add(isStartToggle);

        // Start Id (visible solo si es nodo inicio)
        _startIdField = new TextField("Start Id")
        {
            value = Data.startId ?? string.Empty,
            style = { display = Data.isStart ? DisplayStyle.Flex : DisplayStyle.None }
        };
        _startIdField.style.marginBottom = 3;
        _startIdField.RegisterValueChangedCallback(e => Data.startId = e.newValue);
        fold.Add(_startIdField);

        // Nodo de elección
        fold.Add(new Toggle().BindToggle("Es nodo de elección", Data.isChoiceNode, v =>
        {
            Data.isChoiceNode = v;
            RebuildOutputs();
        }));

        mainContainer.Add(fold);
    }

    // ------------------------------
    // Eventos (foldout independiente)
    // ------------------------------
    private void BuildEventsFoldout()
    {
        var fold = new Foldout { text = "Eventos" };
        fold.viewDataKey = Data.GUID + "_EVENTS";
        fold.style.unityFontStyleAndWeight = FontStyle.Italic;
        fold.style.marginBottom = 3;

        // Event Key
        var eventKeyField = new TextField("Event Key") { value = Data.eventKey };
        eventKeyField.style.marginBottom = 3;
        eventKeyField.RegisterValueChangedCallback(evt =>
        {
            Data.eventKey = evt.newValue;
            ScheduleAutoSize();
        });
        fold.Add(eventKeyField);

        // Selector de tipo
        var typeField = new EnumField("Payload Type", Data.eventPayloadType);
        typeField.style.marginBottom = 3;
        typeField.Init(Data.eventPayloadType);
        typeField.RegisterValueChangedCallback(evt =>
        {
            Data.eventPayloadType = (EventPayloadType)evt.newValue;
            RebuildEventValueField();
            ScheduleAutoSize();
        });
        fold.Add(typeField);

        // Contenedor dinámico (queda en foldout)
        _payloadValueContainer = new VisualElement { name = "payload-value-container" };
        fold.Add(_payloadValueContainer);

        // Fábrica + primer pintado
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
        testBtn.style.marginRight = 2.5f;
        testBtn.style.marginLeft = 4;
        fold.Add(testBtn);

        mainContainer.Add(fold);
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
        paletteDropdown.style.marginBottom = 3;
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

        profileField.style.marginBottom = 3;

        profileField.RegisterValueChangedCallback(e =>
        {
            var so = e.newValue as CharacterProfile;

            Data.profileRef = so;
            Data.profileId = so ? so.ProfileId : null;

            OnDataChanged?.Invoke();
        });

#if UNITY_EDITOR
        // Rehidratación del ObjectField en editor si solo tenemos el ID
        if (Data.profileRef == null && !string.IsNullOrEmpty(Data.profileId))
        {
            var guids = AssetDatabase.FindAssets("t:CharacterProfile");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var so = AssetDatabase.LoadAssetAtPath<CharacterProfile>(path);
                if (so != null && so.ProfileId == Data.profileId)
                {
                    Data.profileRef = so;
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
        extrasFold.style.unityFontStyleAndWeight = FontStyle.Italic;
        extrasFold.style.marginBottom = 3;
        extrasFold.RegisterValueChangedCallback(e => { Data.showExtrasBox = e.newValue; });
        mainContainer.Add(extrasFold);

        // Aparición
        var appearanceField = new EnumField("Aparición", Data.appearance);
        appearanceField.style.marginBottom = 3;
        appearanceField.Init(Data.appearance);
        appearanceField.RegisterValueChangedCallback(e => Data.appearance = (AppearanceMode)e.newValue);
        extrasFold.Add(appearanceField);

        // Origin (Spot) / Target (Spot)
        var originField = new EnumField("Origin", Data.origin);
        originField.style.marginBottom = 3;
        originField.Init(Data.origin);
        originField.RegisterValueChangedCallback(e => Data.origin = (Spot)e.newValue);
        extrasFold.Add(originField);

        var targetField = new EnumField("Target", Data.target);
        targetField.style.marginBottom = 3;
        targetField.Init(Data.target);
        targetField.RegisterValueChangedCallback(e => Data.target = (Spot)e.newValue);
        extrasFold.Add(targetField);

        // Move speed
        var moveSpeedField = new FloatField("Move Speed (px/s)") { value = Data.moveSpeed };
        moveSpeedField.style.marginBottom = 3;
        moveSpeedField.RegisterValueChangedCallback(e => Data.moveSpeed = Mathf.Max(0f, e.newValue));
        extrasFold.Add(moveSpeedField);

        // Inicio del texto
        var textStartField = new EnumField("Inicio del texto", Data.textStart);
        textStartField.style.marginBottom = 3;
        textStartField.Init(Data.textStart);
        textStartField.RegisterValueChangedCallback(e => Data.textStart = (TextStartTiming)e.newValue);
        extrasFold.Add(textStartField);

        // Fade + opacidades
        var useFadeToggle = new Toggle("Usar desvanecido") { value = Data.useFade };
        useFadeToggle.RegisterValueChangedCallback(e => Data.useFade = e.newValue);
        extrasFold.Add(useFadeToggle);

        var enterFrom = new IntegerField("Enter From %") { value = Data.enterFromOpacity };
        enterFrom.style.marginBottom = 3;
        enterFrom.RegisterValueChangedCallback(e => Data.enterFromOpacity = Mathf.Clamp(e.newValue, 0, 100));
        extrasFold.Add(enterFrom);

        var enterTo = new IntegerField("Enter To %") { value = Data.enterToOpacity };
        enterTo.style.marginBottom = 3;
        enterTo.RegisterValueChangedCallback(e => Data.enterToOpacity = Mathf.Clamp(e.newValue, 0, 100));
        extrasFold.Add(enterTo);
    }

    // ------------------------------
    // Animación especial (foldout independiente)
    // ------------------------------
    private void BuildSpecialAnimationFoldout()
    {
        var saFold = new Foldout { text = "Animación especial" };
        saFold.value = false;
        // Usamos viewDataKey para persistir el estado del plegado sin tocar el modelo de datos
        saFold.viewDataKey = Data.GUID + "_SPECIAL_ANIM";
        saFold.style.unityFontStyleAndWeight = FontStyle.Italic;
        saFold.style.marginBottom = 3;

        // Toggle principal: activar/desactivar animación especial
        _specialAnimToggle = new Toggle("Usar animación especial") { value = Data.playSpecialAnimation };
        _specialAnimToggle.RegisterValueChangedCallback(e => Data.playSpecialAnimation = e.newValue);
        saFold.Add(_specialAnimToggle);

        // Clip de animación
        _specialAnimClipField = new EditorObjectField
        {
            label = "Clip",
            objectType = typeof(AnimationClip),
            value = Data.specialAnimation
        };
        _specialAnimClipField.style.marginBottom = 3;
        _specialAnimClipField.RegisterValueChangedCallback(e =>
        {
            Data.specialAnimation = e.newValue as AnimationClip;
        });
        saFold.Add(_specialAnimClipField);

        // Velocidad
        _specialAnimSpeedField = new FloatField("Velocidad") { value = Data.specialAnimSpeed };
        _specialAnimSpeedField.style.marginBottom = 3;
        _specialAnimSpeedField.RegisterValueChangedCallback(e =>
        {
            Data.specialAnimSpeed = Mathf.Max(0f, e.newValue);
        });
        saFold.Add(_specialAnimSpeedField);

        // Loop
        _specialAnimLoopToggle = new Toggle("Loop") { value = Data.specialAnimLoop };
        _specialAnimLoopToggle.RegisterValueChangedCallback(e => Data.specialAnimLoop = e.newValue);
        saFold.Add(_specialAnimLoopToggle);

        // Momento de inicio de la animación especial
        var specialStartField = new EnumField("Inicio anim. especial", Data.specialStart);
        specialStartField.style.marginBottom = 3;
        specialStartField.Init(Data.specialStart);
        specialStartField.RegisterValueChangedCallback(e =>
        {
            Data.specialStart = (SpecialStartTiming)e.newValue;
        });
        saFold.Add(specialStartField);

        // Finalmente añadimos el foldout al contenedor principal
        mainContainer.Add(saFold);
    }

    // ------------------------------
    // Typewriter (foldout)
    // ------------------------------
    private void BuildTypewriterFoldout()
    {
        var typewriterFold = new Foldout { text = "Typewriter" };
        typewriterFold.viewDataKey = Data.GUID + "_TW";
        typewriterFold.value = Data.showTypewriterBox;
        typewriterFold.style.unityFontStyleAndWeight = FontStyle.Italic;
        typewriterFold.style.marginBottom = 3;
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
        _twSecondsPerCharField.style.marginBottom = 3;
        _twSecondsPerCharField.RegisterValueChangedCallback(e => Data.tw.secondsPerChar = Mathf.Clamp(e.newValue, 0.001f, 0.2f));
        typewriterFold.Add(_twSecondsPerCharField);

        _twGlobalSpeedField = new FloatField("Velocidad global (x)") { value = Mathf.Clamp(Data.tw.globalSpeed, 0.1f, 3f) };
        _twGlobalSpeedField.style.marginBottom = 3;
        _twGlobalSpeedField.RegisterValueChangedCallback(e => Data.tw.globalSpeed = Mathf.Clamp(e.newValue, 0.1f, 3f));
        typewriterFold.Add(_twGlobalSpeedField);

        _twRespectRichTextToggle = new Toggle("Respetar RichText") { value = Data.tw.respectRichText };
        _twRespectRichTextToggle.RegisterValueChangedCallback(e => Data.tw.respectRichText = e.newValue);
        typewriterFold.Add(_twRespectRichTextToggle);

        _twWhitespaceDelayToggle = new Toggle("Min. delay en espacios") { value = Data.tw.minimalWhitespaceDelay };
        _twWhitespaceDelayToggle.RegisterValueChangedCallback(e => Data.tw.minimalWhitespaceDelay = e.newValue);
        typewriterFold.Add(_twWhitespaceDelayToggle);

        _twCommaPauseField = new FloatField("Pausa coma (x)") { value = Data.tw.commaPct };
        _twCommaPauseField.style.marginBottom = 3;
        _twCommaPauseField.RegisterValueChangedCallback(e => Data.tw.commaPct = Mathf.Max(0f, e.newValue));
        typewriterFold.Add(_twCommaPauseField);

        _twPeriodPauseField = new FloatField("Pausa punto (x)") { value = Data.tw.periodPct };
        _twPeriodPauseField.style.marginBottom = 3;
        _twPeriodPauseField.RegisterValueChangedCallback(e => Data.tw.periodPct = Mathf.Max(0f, e.newValue));
        typewriterFold.Add(_twPeriodPauseField);

        _twEllipsisPauseField = new FloatField("Pausa '.' (x)") { value = Data.tw.ellipsisPct };
        _twEllipsisPauseField.style.marginBottom = 3;
        _twEllipsisPauseField.RegisterValueChangedCallback(e => Data.tw.ellipsisPct = Mathf.Max(0f, e.newValue));
        typewriterFold.Add(_twEllipsisPauseField);
    }

    private void InitEventFieldFactory()
    {
        _eventFieldFactory = new Dictionary<EventPayloadType, Func<VisualElement>>
        {
            [EventPayloadType.None] = () => null,
            [EventPayloadType.Int] = () =>
            {
                var f = new IntegerField("Value (int)") { value = Data.eventInt };
                f.style.marginBottom = 3;
                f.RegisterValueChangedCallback(e => Data.eventInt = e.newValue);
                return f;
            },
            [EventPayloadType.Float] = () =>
            {
                var f = new FloatField("Value (float)") { value = Data.eventFloat };
                f.style.marginBottom = 3;
                f.RegisterValueChangedCallback(e => Data.eventFloat = e.newValue);
                return f;
            },
            [EventPayloadType.String] = () =>
            {
                var f = new TextField("Value (string)") { value = Data.eventString };
                f.style.marginBottom = 3;
                f.RegisterValueChangedCallback(e => Data.eventString = e.newValue);
                return f;
            },
            [EventPayloadType.Bool] = () =>
            {
                var f = new Toggle("Value (bool)") { value = Data.eventBool };
                f.style.marginBottom = 3;
                f.RegisterValueChangedCallback(e => Data.eventBool = e.newValue);
                return f;
            },
            [EventPayloadType.Char] = () =>
            {
                var initial = string.IsNullOrEmpty(Data.eventString) ? "" : Data.eventString.Substring(0, 1);
                var f = new TextField("Value (char)") { maxLength = 1, value = initial };
                f.style.marginBottom = 3;
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
            countField.style.marginBottom = 3;
            countField.RegisterValueChangedCallback(e =>
            {
                Data.choiceCount = Mathf.Clamp(e.newValue, 2, 4);
                RebuildOutputs();
            });
            outputContainer.Add(countField);

            // Toggle: Mostrar opciones bloqueadas (debajo del número de opciones)
            var showBlockedToggle = new Toggle("Mostrar opciones")
            {
                tooltip = "Si está activo, las opciones que no cumplan requisitos se verán deshabilitadas en runtime. Si está desactivado, se ocultarán.",
                value = Data.showBlockedChoices
            };
            showBlockedToggle.style.marginBottom = 3;
            showBlockedToggle.RegisterValueChangedCallback(e =>
            {
                Data.showBlockedChoices = e.newValue;
            });
            outputContainer.Add(showBlockedToggle);

            for (int i = 0; i < Data.choiceCount; i++)
            {
                // --- Separador antes de cada opción (incluida la primera) ---
                AddThinDividerTo(outputContainer, 0.12f);

                int idx = i;

                // ===================== REQUISITOS (Foldout) =====================
                var reqFold = new Foldout
                {
                    text = "Requisitos"
                };
                // Recuerda el estado por opción
                reqFold.viewDataKey = $"{Data.GUID}_REQ_{i}";
                reqFold.style.unityFontStyleAndWeight = FontStyle.Italic;
                reqFold.style.marginTop = 2;
                reqFold.style.marginBottom = 2;

                // ---------- AFINIDAD ----------
                var affinityColumn = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Column }
                };

                // 1) Cabecera Afinidad (toggle)
                var headerRow = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Row, alignItems = Align.Center }
                };
                var reqToggle = new Toggle("Req. afinidad")
                {
                    value = Data.choices[idx].requiresAffinity
                };
                reqToggle.style.minWidth = 0;
                headerRow.Add(reqToggle);
                affinityColumn.Add(headerRow);

                // 2) Parámetros Afinidad (clave + valor + bajo afinidad)
                var paramsRow = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Column }
                };

                var affinityKeyField = new TextField("Clave")
                {
                    value = Data.choices[idx].affinityKey
                };
                affinityKeyField.style.marginBottom = 3;
                paramsRow.Add(affinityKeyField);
                affinityKeyField.RegisterValueChangedCallback(e => Data.choices[idx].affinityKey = e.newValue);

                var affinityValueField = new FloatField("Valor")
                {
                    value = Data.choices[idx].requiredAffinity
                };
                affinityValueField.style.marginBottom = 3;
                paramsRow.Add(affinityValueField);
                affinityValueField.RegisterValueChangedCallback(e => Data.choices[idx].requiredAffinity = e.newValue);

                var invertToggle = new Toggle("Bajo afinidad")
                {
                    value = Data.choices[idx].invertRequirement
                };
                invertToggle.style.marginBottom = 3;
                paramsRow.Add(invertToggle);
                invertToggle.RegisterValueChangedCallback(e => Data.choices[idx].invertRequirement = e.newValue);

                // Mostrar/ocultar parámetros de afinidad
                void SetAffinityParamsVisible(bool on)
                {
                    paramsRow.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                }
                SetAffinityParamsVisible(Data.choices[idx].requiresAffinity);
                reqToggle.RegisterValueChangedCallback(e =>
                {
                    Data.choices[idx].requiresAffinity = e.newValue;
                    SetAffinityParamsVisible(e.newValue);
                });

                affinityColumn.Add(paramsRow);

                // ---------- PROGRESO ----------
                var progressColumn = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Column }
                };

                // 1) Cabecera Progreso (toggle)
                var progHeaderRow = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Row, alignItems = Align.Center }
                };
                var progToggle = new Toggle("Req. progreso")
                {
                    value = Data.choices[idx].requiresProgress
                };
                progHeaderRow.Add(progToggle);
                progressColumn.Add(progHeaderRow);

                // 2) Parámetros Progreso (método + tipo + valor dinámico)
                var progParamsRow = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Column }
                };

                // Método (string) — dejamos tu configuración de antes (sin tocar el input interno)
                var methodField = new TextField("Método")
                {
                    value = Data.choices[idx].progressMethod
                };
                methodField.style.marginBottom = 3;
                methodField.RegisterValueChangedCallback(e => Data.choices[idx].progressMethod = e.newValue);
                progParamsRow.Add(methodField);

                // Tipo (enum)
                var argTypeField = new EnumField("Arg", Data.choices[idx].progressArgType);
                argTypeField.style.marginBottom = 3;
                argTypeField.Init(Data.choices[idx].progressArgType);
                progParamsRow.Add(argTypeField);

                // Contenedor dinámico del valor
                var argValueContainer = new VisualElement();
                progParamsRow.Add(argValueContainer);

                // Fábrica del campo según tipo
                void RebuildProgressArgField()
                {
                    argValueContainer.Clear();
                    switch (Data.choices[idx].progressArgType)
                    {
                        case ProgressArgType.None:
                            break;
                        case ProgressArgType.Int:
                            {
                                var f = new IntegerField("Valor (int)") { value = Data.choices[idx].progressArgInt };
                                f.style.marginBottom = 3;
                                f.RegisterValueChangedCallback(v => Data.choices[idx].progressArgInt = v.newValue);
                                argValueContainer.Add(f);
                                break;
                            }
                        case ProgressArgType.Float:
                            {
                                var f = new FloatField("Valor (float)") { value = Data.choices[idx].progressArgFloat };
                                f.style.marginBottom = 3;
                                f.RegisterValueChangedCallback(v => Data.choices[idx].progressArgFloat = v.newValue);
                                argValueContainer.Add(f);
                                break;
                            }
                        case ProgressArgType.String:
                            {
                                var f = new TextField("Valor (string)") { value = Data.choices[idx].progressArgString };
                                f.style.marginBottom = 3;
                                f.RegisterValueChangedCallback(v => Data.choices[idx].progressArgString = v.newValue);
                                argValueContainer.Add(f);
                                break;
                            }
                    }
                }

                // Estado inicial
                RebuildProgressArgField();

                // Mostrar/ocultar parámetros de progreso
                void SetProgressParamsVisible(bool on)
                {
                    progParamsRow.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                }
                SetProgressParamsVisible(Data.choices[idx].requiresProgress);

                // Callbacks Progreso
                progToggle.RegisterValueChangedCallback(e =>
                {
                    Data.choices[idx].requiresProgress = e.newValue;
                    SetProgressParamsVisible(e.newValue);
                });
                argTypeField.RegisterValueChangedCallback(e =>
                {
                    Data.choices[idx].progressArgType = (ProgressArgType)e.newValue;
                    RebuildProgressArgField();
                });

                progressColumn.Add(progParamsRow);

                // Separador entre requisitos
                var line = new VisualElement();
                line.style.height = 1;
                line.style.marginTop = 3;
                line.style.marginBottom = 6;
                line.style.backgroundColor = new Color(0, 0, 0, 0.10f);

                // ---------- Montaje dentro del Foldout ----------
                reqFold.Add(affinityColumn);
                reqFold.Add(line);
                reqFold.Add(progressColumn);

                // Añade el foldout al contenedor de outputs
                outputContainer.Add(reqFold);

                // Contenedor vertical de la sección de opción
                var optSection = new VisualElement();
                optSection.style.flexDirection = FlexDirection.Column;
                optSection.style.alignItems = Align.Stretch;

                // Campos que alternan (literal vs clave)
                var choiceTextField = new TextField($"Opción {idx + 1}")
                {
                    value = Data.choices[idx].choiceText
                };
                choiceTextField.style.flexGrow = 1;

                var choiceLocKeyField = new TextField($"Clave localización {idx + 1}")
                {
                    value = Data.choices[idx].choiceLocKey
                };
                choiceLocKeyField.style.flexGrow = 1;

                // Fila horizontal: campo visible + puerto
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;

                // Puerto
                var port = PortUtils.CreatePort(this, Direction.Output, Port.Capacity.Single, Data.choices[idx].portName);

                // Helper tooltip
                System.Action updatePortTooltip = () =>
                {
                    if (Data.choices[idx].choiceUseLocalization)
                        port.tooltip = string.IsNullOrEmpty(Data.choices[idx].choiceLocKey)
                            ? "(loc: vacío)"
                            : $"loc: {Data.choices[idx].choiceLocKey}";
                    else
                        port.tooltip = Data.choices[idx].choiceText;
                };

                // Callbacks campos
                choiceTextField.RegisterValueChangedCallback(e =>
                {
                    Data.choices[idx].choiceText = e.newValue;
                    if (!Data.choices[idx].choiceUseLocalization) updatePortTooltip();
                });
                choiceLocKeyField.RegisterValueChangedCallback(e =>
                {
                    Data.choices[idx].choiceLocKey = e.newValue;
                    if (Data.choices[idx].choiceUseLocalization) updatePortTooltip();
                });

                // Toggle justo DEBAJO del foldout de requisitos
                var choiceLocToggle = new Toggle("Localización")
                {
                    value = Data.choices[idx].choiceUseLocalization
                };
                choiceLocToggle.tooltip = "Activa para usar una clave de localización en esta opción.";
                choiceLocToggle.RegisterValueChangedCallback(e =>
                {
                    Data.choices[idx].choiceUseLocalization = e.newValue;
                    choiceTextField.style.display = e.newValue ? DisplayStyle.None : DisplayStyle.Flex;
                    choiceLocKeyField.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                    updatePortTooltip();
                });

                // Estado inicial + montaje
                choiceTextField.style.display = Data.choices[idx].choiceUseLocalization ? DisplayStyle.None : DisplayStyle.Flex;
                choiceLocKeyField.style.display = Data.choices[idx].choiceUseLocalization ? DisplayStyle.Flex : DisplayStyle.None;
                updatePortTooltip();

                // Orden pedido: (1) Requisitos (ya añadido) -> (2) Toggle loc -> (3) Fila de opción
                optSection.Add(choiceLocToggle);
                row.Add(choiceTextField);
                row.Add(choiceLocKeyField);
                row.Add(port);
                optSection.Add(row);

                // Añade sección completa
                outputContainer.Add(optSection);

                // === Finalmente, la fila principal (texto + puerto) ===
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
