using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using UnityEngine.Playables;
using UnityEngine.Animations;
using TMPro;

public class DialogueRunner : MonoBehaviour
{
    [Header("Graph")]
    public DialogueGraph graph;

    [Header("Controles")]
    public KeyCode advanceKey = KeyCode.N;
    public KeyCode restartKey = KeyCode.R;

    [Header("UI (asigna tus elementos)")]
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI bodyText;
    [Tooltip("Botones de elección (hasta 4) en orden.")]
    public Button[] choiceButtons;

    [Header("Debug")]
    [Tooltip("Si está activo, mostrará en pantalla el GUID del nodo actual.")]
    public bool debugMode = false;

    [Tooltip("Campo de texto (TMP) en el Canvas donde se mostrará el GUID del nodo actual.")]
    public TextMeshProUGUI debugNodeText;

    [Header("Choices - resaltado")]
    [SerializeField] private Color normalChoiceColor = Color.white;
    [SerializeField] private Color visitedChoiceColor = new Color(1f, 0.85f, 0.2f, 1f); // ámbar suave

    [Header("Portrait Highlight")]
    [SerializeField] private bool dimNonSpeaking = true;

    [Header("Portrait Scale")]
    [SerializeField] private bool scaleNonSpeaking = true;

    [Tooltip("Escala del personaje que está hablando.")]
    [SerializeField] private Vector3 speakingScale = Vector3.one;           // 1.00

    [Tooltip("Escala de los personajes que NO están hablando.")]
    [SerializeField] private Vector3 nonSpeakingScale = new Vector3(0.95f, 0.95f, 0.95f); // 0.95

    [Tooltip("Duración del tween de escala (segundos).")]
    [SerializeField, Min(0f)] private float scaleTweenDuration = 0.15f;

    [Tooltip("Curva de interpolación de la escala.")]
    [SerializeField] private AnimationCurve scaleTweenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Color/tinte del personaje que está hablando.")]
    [SerializeField] private Color speakingTint = Color.white;

    [Tooltip("Color/tinte de los personajes que NO están hablando (por ejemplo, más tenue).")]
    [SerializeField] private Color nonSpeakingTint = new Color(1f, 1f, 1f, 0.50f);

    [Header("Override de inicio (opcional)")]
    [SerializeField] private string preferredStartId;   // Coincide con DialogueNodeData.startId
    [SerializeField] private string preferredStartGuid; // Alternativa: seleccionar por GUID exacto

    // (Opcional) propiedades públicas por si prefieres setearlo sin exponer el campo:
    public string PreferredStartId { get => preferredStartId; set => preferredStartId = value; }
    public string PreferredStartGuid { get => preferredStartGuid; set => preferredStartGuid = value; }

    public event Action OnDialogueEnd;

    private readonly Dictionary<string, DialogueNodeData> _nodeByGuid = new();
    private readonly Dictionary<(string fromGuid, string fromPort), string> _edgeLookup = new();
    private readonly Dictionary<string, int> _incomingCount = new();

    private enum LogicalPos { None, Left, Center, Right, OffLeft, OffRight }
    private struct PortraitState
    {
        public LogicalPos logical;   // última “posición lógica” conocida
        public Vector3 screenPos;    // última posición real en pantalla
    }
    private readonly Dictionary<string, PortraitState> _portraitStateByProfile = new();
    
    // Elecciones ya realizadas durante la sesión actual de diálogo
    private readonly HashSet<string> _visitedChoiceKeys = new();

    // --- Typewriter ---
    private readonly TypewriterService _typewriter = new();
    private CancellationTokenSource _twCts;

    private DialogueNodeData _current;
    private bool _waitingChoice;

    // Profile character service
    private ICharacterProfileService _profiles;

    [Header("Localización (opcional)")]
    [SerializeField] private MonoBehaviour localizationServiceRef;
    private ILocalizationService _loc;

    // Referencias UI (ajústalas a tu caso real)
    [Header("UI (opcional)")]
    //[SerializeField] private Text nameText;        // o TMP_Text si usas TMP
    [SerializeField] private Image portraitImage;  // si tienes retrato en UI

    // --- Retratos por perfil (pool dinámico) ---
    [Header("Portraits (pool)")]
    [Tooltip("Contenedor (RectTransform) donde se instancian y gestionan los retratos de cada personaje.")]
    [SerializeField] private RectTransform portraitsRoot;

    // Plantilla para instanciar retratos
    [Tooltip("Plantilla (Image) desactivada que se clona para cada perfil detectado.")]
    [SerializeField] private Image portraitPrefab;

    // Mapeo de perfilId → Image instanciada
    private readonly Dictionary<string, Image> _portraitByProfile = new();
    // Mapeo de perfilId → RectTransform raíz (wrapper) que se mueve por pantalla
    private readonly Dictionary<string, RectTransform> _portraitRootByProfile = new();

    // Playables por retrato (para animaciones especiales por nodo)
    private readonly Dictionary<Image, PlayableGraph> _specialAnimGraphs = new();

    // Control de tweens de escala por retrato (para cancelar el anterior si llega uno nuevo)
    private readonly Dictionary<RectTransform, Coroutine> _scaleTweens = new();

    // Áncoras lógicas de posición (ajústalas a tu layout)
    [Header("Anchors de posición")]
    [Tooltip("Posición destino a la izquierda para las animaciones de entrada/salida.")]
    [SerializeField] private RectTransform leftAnchor;

    [Tooltip("Posición destino centrada para colocar al personaje en pantalla.")]
    [SerializeField] private RectTransform centerAnchor;

    [Tooltip("Posición destino a la derecha para las animaciones de entrada/salida.")]
    [SerializeField] private RectTransform rightAnchor;
    // --- END Retratos por perfil (pool dinámico) ---

    // Animation curves
    [Header("Movimiento de retratos")]
    [Tooltip("Curva de interpolación para el movimiento (0..1). 0: inicio, 1: fin")]
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    // --- END Animation curves ---


    private void Awake()
    {
        DGLog.Info($"DialogueRunner.Awake scene='{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}' " +
               $"isPlaying={Application.isPlaying}");

        // 1) Resolver servicio de perfiles primero
        // REVISAR
        _profiles = (ICharacterProfileService)FindAnyObjectByType<CharacterProfileService>();
        DGLog.Info($"FindAnyObjectByType<CharacterProfileService>() → {(_profiles != null)}");

        if (_profiles == null)
        {
            // Autoinstalar un servicio mínimo si no existe en escena
            var go = new GameObject("_Auto_CharacterProfileService");
            DontDestroyOnLoad(go);
            var svc = go.AddComponent<CharacterProfileService>();
            _profiles = svc;
            DGLog.Warn("Se auto-creó CharacterProfileService en runtime.", go);
        }

        if (graph == null) DGLog.Err("DialogueRunner no tiene 'graph' asignado.");
        else DGLog.Info($"DialogueRunner graph='{graph.name}'");

        // resolver servicio de localización
        _loc = (localizationServiceRef as ILocalizationService) ?? FindAnyObjectByType<CsvLocalizationService>();

        // 2) Validar y arrancar diálogo
        //if (!ValidateGraph()) return;
        //BuildLookups();
        //StartDialogue();

        if (!ValidateGraph()) return;
        BuildLookups();
        BuildPortraitPool();   // NUEVO: crea/activa 1 Image por perfil usado en el graph
        StartDialogue();
    }

    private void OnDestroy()
    {
        _twCts?.Cancel();
        _twCts?.Dispose();
        _twCts = null;
    }

    private void Update()
    {
        if (_current == null) return;

        if (Input.GetKeyDown(restartKey))
        {
            StartDialogue();
            return;
        }

        if (_waitingChoice) return;

        if (!_current.isChoiceNode && Input.GetKeyDown(advanceKey))
        {
            GoNext();
        }
    }

    private bool ValidateGraph()
    {
        if (graph == null)
        {
            Debug.LogError("[DialogueRunner] No hay DialogueGraph asignado.");
            return false;
        }

        _nodeByGuid.Clear();

        foreach (var n in graph.Nodes)
        {
            if (n == null || string.IsNullOrEmpty(n.GUID)) continue;
            if (!_nodeByGuid.ContainsKey(n.GUID))
                _nodeByGuid.Add(n.GUID, n);
        }

        if (_nodeByGuid.Count == 0)
        {
            Debug.LogError("[DialogueRunner] El graph no contiene nodos válidos.");
            return false;
        }

        return true;
    }

    private void BuildLookups()
    {
        _edgeLookup.Clear();
        _incomingCount.Clear();

        foreach (var guid in _nodeByGuid.Keys)
            _incomingCount[guid] = 0;

        foreach (var e in graph.Edges)
        {
            if (e == null || string.IsNullOrEmpty(e.fromNodeGUID) || string.IsNullOrEmpty(e.toNodeGUID))
                continue;

            var fromPort = string.IsNullOrEmpty(e.fromPortName) ? "Next" : e.fromPortName;
            var edgeKey = (fromGuid: e.fromNodeGUID, fromPort: fromPort);

            if (_edgeLookup.ContainsKey(edgeKey))
            {
                Debug.LogWarning($"[DialogueRunner] Puerto duplicado: {edgeKey.fromGuid}:{edgeKey.fromPort}. " +
                                 $"Ya existe conexión hacia GUID={_edgeLookup[edgeKey]}. Ignorando adicional.");
                continue;
            }

            _edgeLookup[edgeKey] = e.toNodeGUID;
            _incomingCount[e.toNodeGUID] = _incomingCount.GetValueOrDefault(e.toNodeGUID) + 1;
        }
    }

    private void ApplyNodeToUI(DialogueNodeData node)
    {
        // ─────────────────────────────────────────────────────────────────────────────
        // Entrada y precondiciones
        // ─────────────────────────────────────────────────────────────────────────────
        DGLog.Info($"ApplyNodeToUI[enter]: node={(node != null)} guid='{node?.GUID}' profileId='{node?.profileId}' portraitKey='{node?.portraitKey}'");

        bool hasLegacy = portraitImage != null;
        bool hasPool = _portraitByProfile != null && _portraitByProfile.Count > 0;

        if (!hasLegacy && !hasPool)
        {
            // Ni imagen legacy ni pool configurado → no hay destino donde aplicar el sprite
            DGLog.Warn("ApplyNodeToUI: NO legacy portraitImage y NO pool (_portraitByProfile vacío o null). Salgo.");
            return;
        }

        if (node == null)
        {
            DGLog.Err("ApplyNodeToUI: node == null");
            return;
        }

        if (_profiles == null)
        {
            DGLog.Err("ApplyNodeToUI: _profiles == null (CharacterProfileService no disponible).");
            return;
        }

        if (string.IsNullOrEmpty(node.profileId))
        {
            DGLog.Err($"ApplyNodeToUI: node.profileId vacío o null. GUID={node.GUID}");
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Resolución de perfil y selección de sprite
        // ─────────────────────────────────────────────────────────────────────────────
        Sprite sprite = null;

        if (!string.IsNullOrEmpty(node.profileId))
        {
            DGLog.Info($"ApplyNodeToUI: Intentando resolver perfil por id='{node.profileId}' …");
            var profile = _profiles.GetById(node.profileId);

            if (profile != null)
            {
                DGLog.Info($"ApplyNodeToUI: profile OK → '{profile.name}' displayName='{profile.DisplayName}'");

                // 1) Retrato por clave explícita
                if (!string.IsNullOrEmpty(node.portraitKey))
                {
                    sprite = profile.GetPortraitByKey(node.portraitKey);
                    DGLog.Info($"ApplyNodeToUI: key explícita '{node.portraitKey}' → {(sprite ? sprite.name : "NULL")}");
                }

                // 2) Fallback "Default"
                if (sprite == null)
                {
                    sprite = profile.GetPortraitByKey("Default");
                    DGLog.Info($"ApplyNodeToUI: fallback 'Default' → {(sprite ? sprite.name : "NULL")}");
                }

                // 3) Fallback al primer retrato disponible
                if (sprite == null)
                {
                    var firstKey = profile.GetPortraitKeys().FirstOrDefault();
                    if (!string.IsNullOrEmpty(firstKey))
                    {
                        sprite = profile.GetPortraitByKey(firstKey);
                        DGLog.Info($"ApplyNodeToUI: fallback primer retrato '{firstKey}' → {(sprite ? sprite.name : "NULL")}");
                    }
                    else
                    {
                        DGLog.Warn("ApplyNodeToUI: el perfil no tiene retratos (GetPortraitKeys vacío).");
                    }
                }
            }
            else
            {
                DGLog.Err($"ApplyNodeToUI: profile NULL para id='{node.profileId}'");
            }
        }

        if (sprite == null)
        {
            DGLog.Warn($"ApplyNodeToUI: SIN sprite final (guid='{node.GUID}', profileId='{node.profileId}', portraitKey='{node.portraitKey}')");
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Aplicación a UI: modo legacy vs pool por perfil
        // ─────────────────────────────────────────────────────────────────────────────

        // 1) Legacy (si no hay pool)
        if (!hasPool && hasLegacy)
        {
            DGLog.Info($"ApplyNodeToUI[legacy]: set sprite={(sprite ? sprite.name : "NULL")} enabled={(sprite != null)}");
            portraitImage.sprite = sprite;
            portraitImage.enabled = sprite != null; // oculta si no hay sprite

            // --- SPECIAL ANIMATION (por nodo) ---
            // Obtenemos el Image del perfil activo (ruta legacy = portraitImage)
            Image activeImg = portraitImage;
            // Si cambiamos de nodo, paramos la animación previa en este retrato
            StopSpecialAnimation(activeImg);
            if (node.playSpecialAnimation && node.specialAnimation != null)
            {
                PlaySpecialAnimation(activeImg, node.specialAnimation, node.specialAnimSpeed, node.specialAnimLoop);
            }

            return;
        }

        // 2) Pool por perfil
        if (hasPool)
        {
            if (!string.IsNullOrEmpty(node.profileId))
            {
                if (_portraitByProfile.TryGetValue(node.profileId, out var img))
                {
                    DGLog.Info($"ApplyNodeToUI[pool]: profileId='{node.profileId}' set sprite={(sprite ? sprite.name : "NULL")} enabled={(sprite != null)}");
                    img.sprite = sprite;
                    img.enabled = sprite != null;

                    // --- SPECIAL ANIMATION (por nodo) ---
                    // Obtenemos el Image del perfil activo (ruta pool = img del diccionario)
                    Image activeImg = img;
                    // Si cambiamos de nodo, paramos la animación previa en este retrato
                    StopSpecialAnimation(activeImg);
                    if (node.playSpecialAnimation && node.specialAnimation != null)
                    {
                        PlaySpecialAnimation(activeImg, node.specialAnimation, node.specialAnimSpeed, node.specialAnimLoop);
                    }
                }
                else
                {
                    DGLog.Warn($"ApplyNodeToUI[pool]: NO hay Image mapeado en _portraitByProfile para profileId='{node.profileId}'");
                }
            }
            else
            {
                DGLog.Warn("ApplyNodeToUI[pool]: profileId vacío; no se puede resolver Image del pool.");
            }
        }

        DGLog.Info("ApplyNodeToUI[exit]");
    }


    private void StartDialogue()
    {
        _visitedChoiceKeys.Clear(); // reinicia el historial de elecciones para esta sesión
        DialogueNodeData start = null;

        // 0) Overrides externos (si están bien formados y apuntan a nodos de inicio)
        if (!string.IsNullOrEmpty(preferredStartGuid) &&
            _nodeByGuid.TryGetValue(preferredStartGuid, out var byGuid) &&
            byGuid.isStart)
        {
            start = byGuid;
        }
        else if (!string.IsNullOrEmpty(preferredStartId))
        {
            start = _nodeByGuid.Values
                .FirstOrDefault(n => n.isStart && string.Equals(n.startId, preferredStartId, StringComparison.OrdinalIgnoreCase));
        }

        // 1) Si no hubo override válido, usa cualquier nodo marcado como inicio (sin warning)
        if (start == null)
        {
            var starts = _nodeByGuid.Values.Where(n => n.isStart).ToList();
            if (starts.Count >= 1)
                start = starts[0];
        }

        // 2) Si no hay isStart, elige uno sin entradas
        if (start == null)
            start = _nodeByGuid.Values.FirstOrDefault(n => _incomingCount.TryGetValue(n.GUID, out var c) && c == 0);

        // 3) Fallback absoluto
        if (start == null)
            start = _nodeByGuid.Values.First();


        SetCurrent(start);
    }

    private void SetCurrent(DialogueNodeData node)
    {
        // 0) Actualizar referencia actual y limpiar estado
        _current = node;

        // 0.5) Eventos de entrada
        if (_current != null)
        {
            if (!string.IsNullOrEmpty(_current.eventKey))
            {
                // NUEVO: payload tipado
                var payload = _current.BuildEventPayload();
                GlobalDialogueEvents.Fire(payload);

                // Legacy: ya se lanza dentro del Fire(payload) por compatibilidad
            }

            _current.onEnter?.Invoke();
        }

        UpdateDebugLabel();
        _waitingChoice = false;
        HideChoices(); // por si venimos de un nodo de elección

        // 1) Speaker (nombre)
        if (speakerText) speakerText.text = GetSpeakerName(_current);

        // 2) ¿El texto empieza antes de la animación o después?
        bool startTextNow = _current != null && _current.textStart == TextStartTiming.BeforeAnimation;

        // 3) Sprite del perfil del nodo
        ApplyNodeToUI(_current);

        // 3.1) Asegurar que el retrato del hablante quede por encima del resto
        BringPortraitOnTop(_current?.profileId);

        // 3.2) Atenuar/no atenuar retratos según quién hable
        UpdatePortraitHighlight(_current?.profileId);
        UpdatePortraitScale(_current?.profileId);

        // 3.3) Escalar retratos: hablante vs no-hablantes (con tween y curva)
        UpdatePortraitScale(_current?.profileId);

        // 4) Anim / Placement
        RunCharacterPlacement(_current, onAnimDone: () =>
        {
            // 4.1) Mostrar el texto si estaba esperando al final de la animación
            if (!startTextNow)
                DisplayNodeBodyAsync(_current);

            // 4.2) Si este nodo es de elección, mostramos las opciones ahora
            if (_current != null && _current.isChoiceNode)
                ShowChoices(_current);
        });

        // 5) Si el texto debe empezar ANTES de la animación:
        if (startTextNow)
            DisplayNodeBodyAsync(_current);

        // 6) Si el nodo no es de elección, nos quedamos a la espera de la tecla avanzar/edges
        // (la lógica de Update y GoNext ya se encarga)
    }

    // Sube a tope de la jerarquía el retrato del perfil indicado
    private void BringPortraitOnTop(string profileId)
    {
        if (string.IsNullOrEmpty(profileId)) return;
        if (portraitsRoot == null) return;

        if (_portraitByProfile != null && _portraitByProfile.TryGetValue(profileId, out var img) && img != null)
        {
            // Esto controla el orden de render en la UI (último hijo = arriba del todo)
            img.transform.SetAsLastSibling();
        }
    }

    private void RunCharacterPlacement(DialogueNodeData node, Action onAnimDone)
    {
        // 0) Validaciones + obtener la Image y el ROOT del perfil
        if (node == null || string.IsNullOrEmpty(node.profileId) ||
            _portraitByProfile == null || !_portraitByProfile.TryGetValue(node.profileId, out var img) ||
            img == null)
        {
            onAnimDone?.Invoke();
            return;
        }

        // ROOT (wrapper) que se mueve por pantalla
        if (!_portraitRootByProfile.TryGetValue(node.profileId, out var rootRt) || rootRt == null)
        {
            onAnimDone?.Invoke();
            return;
        }

        // 1) CanvasGroup (fade) en el ROOT
        var cg = rootRt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rootRt.gameObject.AddComponent<CanvasGroup>();

        // 2) Posición de ORIGEN (sobre ROOT)
        Vector3 startPos;
        if (node.origin == Spot.Auto && _portraitStateByProfile.TryGetValue(node.profileId, out var prev))
            startPos = prev.screenPos;
        else
            startPos = ResolveSpot(node.origin, rootRt);

        rootRt.position = startPos;

        // 3) Fades (entrada vs salida según target)
        float from, to;
        bool exiting = (node.target == Spot.OffLeft || node.target == Spot.OffRight);
        if (node.useFade)
        {
            if (exiting) { from = node.exitFromOpacity / 100f; to = node.exitToOpacity / 100f; }
            else { from = node.enterFromOpacity / 100f; to = node.enterToOpacity / 100f; }
        }
        else { from = to = 1f; }
        cg.alpha = from;

        // 4) Destino
        if (node.target == Spot.Keep)
        {
            if (node.useFade) cg.alpha = node.enterToOpacity / 100f;

            _portraitStateByProfile[node.profileId] = new PortraitState
            {
                logical = LogicalPos.None,
                screenPos = rootRt.position
            };
            onAnimDone?.Invoke();
            return;
        }

        Vector3 endPos = ResolveSpot(node.target, rootRt);

        // 5) Animar o teletransportar (sobre ROOT)
        bool doTeleport = (node.appearance == AppearanceMode.Preplaced) || (node.moveSpeed <= 1f);

        Action persistAndDone = () =>
        {
            _portraitStateByProfile[node.profileId] = new PortraitState
            {
                logical = ResolveLogical(node.target),
                screenPos = rootRt.position
            };
            onAnimDone?.Invoke();
        };

        if (doTeleport)
        {
            rootRt.position = endPos;
            cg.alpha = to;
            persistAndDone();
        }
        else
        {
            StartCoroutine(SlideAndFade(rootRt, cg, endPos, to, node.moveSpeed, persistAndDone));
        }
    }

    private System.Collections.IEnumerator SlideAndFade(RectTransform rt, CanvasGroup cg, Vector3 endPos, float endAlpha, float speed, Action onDone)
    {
        // Posición y alpha de partida
        Vector3 startPos = rt.position;
        float startAlpha = cg.alpha;

        // Si la velocidad es muy baja, teletransportamos (mismo comportamiento previo)
        if (speed <= 1f)
        {
            rt.position = endPos;
            cg.alpha = endAlpha;
            onDone?.Invoke();
            yield break;
        }

        // Duración total = distancia / velocidad (respetamos la velocidad que dicta el nodo)
        float totalDist = Vector3.Distance(startPos, endPos);
        if (totalDist <= Mathf.Epsilon)
        {
            // No hay movimiento, solo fade
            float t0 = 0f;
            while (t0 < 1f)
            {
                t0 = Mathf.Clamp01(t0 + Time.deltaTime); // ~1s de fade lineal si no hay distancia
                cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t0);
                yield return null;
            }
            onDone?.Invoke();
            yield break;
        }

        float duration = totalDist / speed; // clave: respeta moveSpeed del nodo
        float elapsed = 0f;

        // Seguridad: si no hay curva definida, usamos interpolación lineal
        AnimationCurve curve = moveCurve != null ? moveCurve : AnimationCurve.Linear(0f, 0f, 1f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Progreso "crudo" 0..1 basado en tiempo (mantiene la misma duración total)
            float rawT = Mathf.Clamp01(elapsed / duration);

            // Progreso "suavizado" por la curva del inspector (solo afecta a la posición)
            float easedT = Mathf.Clamp01(curve.Evaluate(rawT));

            // Movimiento con curva
            rt.position = Vector3.LerpUnclamped(startPos, endPos, easedT);

            // Alpha sigue lineal (si quieres que también use la curva, cambia rawT -> easedT)
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, rawT);
            //cg.alpha = Mathf.Lerp(startAlpha, endAlpha, easedT); // opcional: alpha también con curva

            yield return null;
        }

        // Aseguramos estado final exacto
        rt.position = endPos;
        cg.alpha = endAlpha;
        onDone?.Invoke();
    }

    private void BuildPortraitPool()
    {
        if (portraitsRoot == null || portraitPrefab == null) return;

        // Limpiar previos
        foreach (var kv in _portraitByProfile)
            if (kv.Value) Destroy(kv.Value.gameObject);
        _portraitByProfile.Clear();

        foreach (var kv in _portraitRootByProfile)
            if (kv.Value) Destroy(kv.Value.gameObject);
        _portraitRootByProfile.Clear();

        // Perfiles únicos usados en nodos del graph
        var uniqueProfiles = graph.Nodes
            .Where(n => n != null && !string.IsNullOrEmpty(n.profileId))
            .Select(n => n.profileId)
            .Distinct();

        foreach (var pid in uniqueProfiles)
        {
            // 1) Crear ROOT (wrapper) vacío
            var rootGo = new GameObject($"PortraitRoot_{pid}", typeof(RectTransform), typeof(CanvasGroup));
            var rootRt = (RectTransform)rootGo.transform;
            rootRt.SetParent(portraitsRoot, worldPositionStays: false);

            // Colocación inicial: centrado (el nodo dictará target/anims)
            if (centerAnchor != null)
            {
                rootRt.position = centerAnchor.position;
            }
            else
            {
                rootRt.anchorMin = new Vector2(0.5f, 0.5f);
                rootRt.anchorMax = new Vector2(0.5f, 0.5f);
                rootRt.anchoredPosition = Vector2.zero;
            }
            rootRt.localScale = Vector3.one;
            var cg = rootGo.GetComponent<CanvasGroup>();
            cg.alpha = 0f; // arrancan ocultos hasta que tengan sprite y se coloquen

            // 2) Instanciar la Image (HIJO) que contendrá sprite y animaciones especiales
            var img = Instantiate(portraitPrefab, rootRt);
            img.gameObject.name = $"Portrait_{pid}";
            img.gameObject.SetActive(true);
            img.enabled = false; // hasta que tenga sprite

            // Aseguramos transform local limpio para animaciones relativas
            var imgRt = img.rectTransform;
            imgRt.anchorMin = new Vector2(0.5f, 0.5f);
            imgRt.anchorMax = new Vector2(0.5f, 0.5f);
            imgRt.pivot = new Vector2(0.5f, 0.5f);
            imgRt.anchoredPosition = Vector2.zero;
            imgRt.localPosition = Vector3.zero;
            imgRt.localRotation = Quaternion.identity;
            imgRt.localScale = Vector3.one;

            _portraitByProfile[pid] = img;      // para sprites/tintes/animación especial
            _portraitRootByProfile[pid] = rootRt;   // para placement (slide/fade)
            _portraitStateByProfile[pid] = new PortraitState { logical = LogicalPos.None, screenPos = rootRt.position };
        }

        // Si mantenemos soporte para portraitImage "legacy", lo ocultamos por defecto:
        if (portraitImage) { portraitImage.enabled = false; }
    }

    // Aplica (y anima) la escala a todos los retratos del pool en función del hablante actual.
    // - Si scaleNonSpeaking == false: deja todos con speakingScale (sin diferenciar).
    // - Si currentProfileId es null o vacío: deja todos con speakingScale (sin atenuar escala).
    private void UpdatePortraitScale(string currentProfileId)
    {
        if (_portraitByProfile == null || _portraitByProfile.Count == 0)
        {
            // Ruta legacy: un solo retrato
            if (portraitImage != null)
            {
                var rt = portraitImage.rectTransform;
                StartScaleTween(rt, speakingScale);
            }
            return;
        }

        bool hasSpeaker = !string.IsNullOrEmpty(currentProfileId);

        foreach (var kv in _portraitByProfile)
        {
            var img = kv.Value;
            if (img == null) continue;
            var rt = img.rectTransform;

            if (!scaleNonSpeaking || !hasSpeaker)
            {
                StartScaleTween(rt, speakingScale);
                continue;
            }

            var target = (kv.Key == currentProfileId) ? speakingScale : nonSpeakingScale;
            StartScaleTween(rt, target);
        }
    }

    private void StartScaleTween(RectTransform rt, Vector3 targetScale)
    {
        if (rt == null) return;

        // Si había un tween en curso, lo paramos
        if (_scaleTweens.TryGetValue(rt, out var running) && running != null)
        {
            StopCoroutine(running);
        }

        // Si la duración es 0 o negativa, aplicamos instantáneo
        if (scaleTweenDuration <= 0f)
        {
            rt.localScale = targetScale;
            _scaleTweens.Remove(rt);
            return;
        }

        var co = StartCoroutine(TweenScaleCoroutine(rt, targetScale, scaleTweenDuration, scaleTweenCurve));
        _scaleTweens[rt] = co;
    }

    private System.Collections.IEnumerator TweenScaleCoroutine(RectTransform rt, Vector3 to, float duration, AnimationCurve curve)
    {
        Vector3 from = rt.localScale;
        float t = 0f;

        // Usamos deltaTime normal; si tu UI va en pausa, puedes cambiar a unscaledDeltaTime
        while (t < duration && rt != null)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float k = (curve != null) ? curve.Evaluate(u) : u;
            rt.localScale = Vector3.LerpUnclamped(from, to, k);
            yield return null;
        }

        if (rt != null) rt.localScale = to;

        // Limpiamos referencia del tween terminado
        if (rt != null) _scaleTweens.Remove(rt);
    }

    private void StopSpecialAnimation(Image img)
    {
        if (img == null) return;
        if (_specialAnimGraphs.TryGetValue(img, out var g))
        {
            if (g.IsValid()) g.Destroy();
            _specialAnimGraphs.Remove(img);
        }
    }

    /// <summary>
    /// Arranca usando el graph ya asignado en el componente.
    /// Puedes forzar inicio por startId o por GUID.
    /// </summary>
    public void Play(string startId = null, string startGuid = null)
    {
        if (startId != null) preferredStartId = startId;
        if (startGuid != null) preferredStartGuid = startGuid;

        if (!ValidateGraph()) return;
        BuildLookups();       // tu método actual que llena _nodeByGuid, etc.
        BuildPortraitPool();  // si lo tienes; deja tal cual tu pipeline
        StartDialogue();      // usa la selección mejorada (sección C)
    }

    /// <summary>
    /// Igual que el anterior pero asignando el graph primero.
    /// </summary>
    public void Play(DialogueGraph g, string startId = null, string startGuid = null)
    {
        graph = g;
        Play(startId, startGuid);
    }

    private void PlaySpecialAnimation(Image img, AnimationClip clip, float speed = 1f, bool loop = true)
    {
        if (img == null || clip == null) return;

        // Asegura un Animator en el retrato (Playables lo necesita como output target)
        var animator = img.GetComponent<Animator>();
        if (animator == null) animator = img.gameObject.AddComponent<Animator>();

        // Limpia cualquier animación previa
        StopSpecialAnimation(img);

        // Crea el Graph
        var graph = PlayableGraph.Create($"DG_SpecialAnim_{img.name}");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        var playableOutput = AnimationPlayableOutput.Create(graph, "AnimOutput", animator);
        var clipPlayable = AnimationClipPlayable.Create(graph, clip);

        // Loop: respetamos la importación del clip; si “loop” está activo, nos aseguramos de que no se “pare”
        clipPlayable.SetSpeed(Mathf.Approximately(speed, 0f) ? 0f : speed);

        playableOutput.SetSourcePlayable(clipPlayable);

        // Arranca
        graph.Play();
        _specialAnimGraphs[img] = graph;

        // Si no es loop, destruye al terminar (best-effort)
        if (!loop && clip.length > 0f && speed > 0f)
        {
            StartCoroutine(StopGraphWhenDone(graph, (float)(clip.length / speed)));
        }
    }

    private System.Collections.IEnumerator StopGraphWhenDone(PlayableGraph g, float delay)
    {
        float t = 0f;
        while (t < delay && g.IsValid())
        {
            t += Time.deltaTime;
            yield return null;
        }
        if (g.IsValid()) g.Destroy();
    }


    // Aplica el tinte a todos los retratos del pool en función del hablante actual.
    // - Si dimNonSpeaking == false, no modifica colores (deja todos con speakingTint)
    // - Si currentProfileId es null o vacío, deja todos con speakingTint (sin atenuar)
    private void UpdatePortraitHighlight(string currentProfileId)
    {
        // Si no hay pool, intenta ruta legacy (portraitImage único)
        if (_portraitByProfile == null || _portraitByProfile.Count == 0)
        {
            if (portraitImage != null)
            {
                portraitImage.color = speakingTint; // único retrato, sin dimming real
            }
            return;
        }

        bool hasSpeaker = !string.IsNullOrEmpty(currentProfileId);

        foreach (var kv in _portraitByProfile)
        {
            var img = kv.Value;
            if (img == null) continue;

            if (!dimNonSpeaking)
            {
                // Efecto desactivado: todos con el color "hablando"
                img.color = speakingTint;
                continue;
            }

            // Si no hay hablante claro, no atenuamos a nadie
            if (!hasSpeaker)
            {
                img.color = speakingTint;
                continue;
            }

            // Tinte según si es el que habla o no
            img.color = (kv.Key == currentProfileId) ? speakingTint : nonSpeakingTint;
        }
    }

    private async void DisplayNodeBodyAsync(DialogueNodeData node)
    {
        if (bodyText == null)
            return;

        // Texto resuelto (localización incluida)
        string nodeText = ResolveBodyText(node);

        // Si no hay Typewriter, mostrar instantáneo
        // (si tu DialogueNodeData aún no tiene useTypewriter, añade ese bool en tu modelo)
        if (node == null || !node.useTypewriter)
        {
            bodyText.SetText(nodeText);
            return;
        }

        // Preparar CTS y cancelar la animación previa si la hubiera
        _twCts?.Cancel();
        _twCts?.Dispose();
        _twCts = new CancellationTokenSource();

        // Construir perfil desde los overrides del nodo (mínimo y claro)
        var p = ScriptableObject.CreateInstance<TypewriterProfile>();
        p.secondsPerChar = node.tw.secondsPerChar;
        p.globalSpeed = node.tw.globalSpeed;
        p.respectRichText = node.tw.respectRichText;
        p.minimalWhitespaceDelay = node.tw.minimalWhitespaceDelay;

        // pausas comunes (si no las tienes en tu struct, elimínalas o añádelas)
        p.commaPct = node.tw.commaPct;
        p.periodPct = node.tw.periodPct;
        p.ellipsisPct = node.tw.ellipsisPct;

        try
        {
            await _typewriter.RunAsync(nodeText, p, bodyText, null, _twCts.Token);
            // Al terminar de escribir, si el nodo actual es de elección y seguimos en él, muestra opciones
            if (_current == node && node.isChoiceNode)
            {
                ShowChoices(node);
            }
        }
        catch (OperationCanceledException)
        {
            // Cambio de nodo/skip: ignorar
        }
    }


    private string ResolveBodyText(DialogueNodeData node)
    {
        if (node == null) return string.Empty; // seguridad

        // si el nodo usa clave localizada
        if (node.localization && !string.IsNullOrEmpty(node.locKey) && _loc != null)
        {
            if (_loc.TryGet(node.locKey, out var localizedText))
                return localizedText;
            else
                Debug.LogWarning($"[DialogueRunner] Clave de localización no encontrada: {node.locKey}");
        }

        return GetNodeText(node);
    }

    private void ShowChoices(DialogueNodeData node)
    {
        _waitingChoice = true;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            var btn = choiceButtons[i];
            if (btn == null) continue;
            btn.onClick.RemoveAllListeners();

            if (node.choices != null && i < node.choices.Count)
            {
                var choice = node.choices[i];
                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.text = string.IsNullOrEmpty(choice.choiceText) ? $"Opción {i + 1}" : choice.choiceText;

                var port = string.IsNullOrEmpty(choice.portName) ? $"choice_{i}" : choice.portName;

                // Resalte si la opción ya fue escogida previamente en esta sesión
                var visited = _visitedChoiceKeys.Contains(MakeChoiceKey(node.GUID, port));
                // 1) Cambiar el color del Image del propio Button (Source Image)
                var bg = btn.image; // Image en el mismo GameObject del Button
                if (bg) bg.color = visited ? visitedChoiceColor : normalChoiceColor;

                btn.onClick.AddListener(() => OnChoiceSelected(node.GUID, port));
                btn.gameObject.SetActive(true);
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }

    private void HideChoices()
    {
        foreach (var b in choiceButtons)
            if (b) b.gameObject.SetActive(false);
    }

    private void OnChoiceSelected(string fromGuid, string fromPort)
    {
        _waitingChoice = false;
        _visitedChoiceKeys.Add(MakeChoiceKey(fromGuid, fromPort)); // registrar como ya escogida
        var key = (fromGuid, fromPort);

        if (_edgeLookup.TryGetValue(key, out var toGuid) && _nodeByGuid.TryGetValue(toGuid, out var toNode))
            SetCurrent(toNode);
        else
            EndDialogue();
    }

    // Clave única por opción: nodoGUID + puerto
    private static string MakeChoiceKey(string guid, string port) => $"{guid}::{port}";

    private void GoNext()
    {
        if (_current == null) return;

        var key = (_current.GUID, "Next");
        if (_edgeLookup.TryGetValue(key, out var toGuid) && _nodeByGuid.TryGetValue(toGuid, out var toNode))
        {
            SetCurrent(toNode);
            return;
        }

        var fallback = _edgeLookup.FirstOrDefault(kv => kv.Key.fromGuid == _current.GUID).Value;
        if (!string.IsNullOrEmpty(fallback) && _nodeByGuid.TryGetValue(fallback, out var to2))
            SetCurrent(to2);
        else
            EndDialogue();
    }

    private void EndDialogue()
    {
        HideChoices();
        if (speakerText) speakerText.text = "";
        if (bodyText) bodyText.text = "<i>(Fin del diálogo)</i>";

        if (portraitImage)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }

        // Ocultar todos los retratos del pool
        foreach (var kv in _portraitByProfile)
        {
            if (kv.Value)
            {
                kv.Value.enabled = false;
                var cg = kv.Value.GetComponent<CanvasGroup>();
                if (cg) cg.alpha = 0f;
            }
        }

        _current = null;
        UpdateDebugLabel();
        _waitingChoice = false;
        OnDialogueEnd?.Invoke();
    }

    // --- DEBUG ---
    private void UpdateDebugLabel()
    {
        if (debugNodeText == null)
            return;

        // Activa/oculta el objeto según el toggle
        debugNodeText.gameObject.SetActive(debugMode);

        if (!debugMode)
            return;

        // Muestra el GUID del nodo actual o un marcador si no hay
        var id = _current != null ? _current.GUID : "(end/null)";
        debugNodeText.text = $"Node GUID: {id}";
    }

    // Getter público opcional (útil si otro script necesita el GUID actual)
    public string CurrentNodeGuid => _current != null ? _current.GUID : null;


    // --- Helpers de compatibilidad de nombres ---
    private static string GetNodeText(object node)
        => TryGetStringField(node, "text", "lineText", "dialogueText", "dialogText", "content", "body");

    private static string GetSpeakerName(object node)
        => TryGetStringField(node, "speakerName", "speaker", "character", "name");
    //private string GetSpeakerName(DialogueNodeData node)
    //{
    //    if (node == null) return string.Empty;

    //    if (_profiles != null && !string.IsNullOrEmpty(node.profileId))
    //    {
    //        var profile = _profiles.GetById(node.profileId);
    //        if (profile != null && !string.IsNullOrEmpty(profile.displayName))
    //            return profile.displayName; // prioriza el nombre del perfil
    //    }

    //    return node.speakerName; // fallback al del nodo
    //}

    private static string TryGetStringField(object obj, params string[] names)
    {
        if (obj == null) return "";
        var t = obj.GetType();
        foreach (var n in names)
        {
            var f = t.GetField(n);
            if (f != null && f.FieldType == typeof(string))
                return (string)(f.GetValue(obj) ?? "");
            var p = t.GetProperty(n);
            if (p != null && p.PropertyType == typeof(string))
                return (string)(p.GetValue(obj) ?? "");
        }
        return "";
    }

    private Vector3 GetAnchorPos(RectTransform anchor, Vector3 fallback)
    => anchor ? anchor.position : fallback;

    private Vector3 GetOffscreenPos(Vector3 around, bool toLeft, float offsetPx)
    {
        var p = around;
        p.x += toLeft ? -offsetPx : offsetPx;
        return p;
    }

    private Vector3 ResolveSpot(Spot spot, RectTransform rt)
    {
        switch (spot)
        {
            case Spot.Left: return GetAnchorPos(leftAnchor, rt.position);
            case Spot.Center: return GetAnchorPos(centerAnchor, rt.position);
            case Spot.Right: return GetAnchorPos(rightAnchor, rt.position);
            case Spot.OffLeft: return GetOffscreenPos(GetAnchorPos(leftAnchor, rt.position), true, Screen.width * 0.6f);
            case Spot.OffRight: return GetOffscreenPos(GetAnchorPos(rightAnchor, rt.position), false, Screen.width * 0.6f);
            case Spot.Auto:
            case Spot.Keep:
            default: return rt.position; // se resuelve antes con estado previo
        }
    }

    private LogicalPos ResolveLogical(Spot s)
    {
        switch (s)
        {
            case Spot.Left: return LogicalPos.Left;
            case Spot.Center: return LogicalPos.Center;
            case Spot.Right: return LogicalPos.Right;
            case Spot.OffLeft: return LogicalPos.OffLeft;
            case Spot.OffRight: return LogicalPos.OffRight;
            default: return LogicalPos.None;
        }
    }
}
