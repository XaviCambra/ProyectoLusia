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
    [SerializeField] private Color visitedChoiceColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Header("Portrait Highlight")]
    [SerializeField] private bool dimNonSpeaking = true;
    [SerializeField] private Color speakingTint = Color.white;
    [SerializeField] private Color nonSpeakingTint = new Color(1f, 1f, 1f, 0.50f);

    [Header("Portrait Scale")]
    [SerializeField] private bool scaleNonSpeaking = true;
    [SerializeField] private Vector3 speakingScale = Vector3.one;
    [SerializeField] private Vector3 nonSpeakingScale = new Vector3(0.95f, 0.95f, 0.95f);
    [SerializeField, Min(0f)] private float scaleTweenDuration = 0.15f;
    [SerializeField] private AnimationCurve scaleTweenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Override de inicio (opcional)")]
    [SerializeField] private string preferredStartId;
    [SerializeField] private string preferredStartGuid;
    public string PreferredStartId { get => preferredStartId; set => preferredStartId = value; }
    public string PreferredStartGuid { get => preferredStartGuid; set => preferredStartGuid = value; }

    public event Action OnDialogueEnd;

    private readonly Dictionary<String, DialogueNodeData> _nodeByGuid = new();
    private readonly Dictionary<(string fromGuid, string fromPort), string> _edgeLookup = new();
    private readonly Dictionary<string, int> _incomingCount = new();

    private enum LogicalPos { None, Left, Center, Right, OffLeft, OffRight }
    private struct PortraitState { public LogicalPos logical; public Vector3 screenPos; }
    private readonly Dictionary<string, PortraitState> _portraitStateByProfile = new();

    private readonly HashSet<string> _visitedChoiceKeys = new();

    // --- Typewriter ---
    private readonly TypewriterService _typewriter = new();
    private CancellationTokenSource _twCts;

    // --- Typewriter state
    private string _currentNodeFullText;
    private bool _isTypewriting;

    private DialogueNodeData _current;
    private bool _waitingChoice;

    // Estado interno de animación/texto
    private bool _animDoneForCurrentNode;
    private bool _textDoneForCurrentNode;

    // Profile character service
    private ICharacterProfileService _profiles;

    [Header("Localización (opcional)")]
    [SerializeField] private MonoBehaviour localizationServiceRef;
    private ILocalizationService _loc;

    [Header("UI (opcional)")]
    [SerializeField] private Image portraitImage;  // soporte legacy (un retrato)

    // --- Retratos por perfil (pool dinámico) ---
    [Header("Portraits (pool)")]
    [SerializeField] private RectTransform portraitsRoot;
    [SerializeField] private Image portraitPrefab;
    private readonly Dictionary<string, Image> _portraitByProfile = new();
    private readonly Dictionary<string, RectTransform> _portraitRootByProfile = new();
    private readonly Dictionary<Image, PlayableGraph> _specialAnimGraphs = new();
    private readonly Dictionary<RectTransform, Coroutine> _scaleTweens = new();

    // Tweens de movimiento/fade de colocación por retrato (para poder cancelarlos/fast-forward)
    private readonly Dictionary<RectTransform, Coroutine> _moveTweens = new();

    [Header("Anchors de posición")]
    [SerializeField] private RectTransform leftAnchor;
    [SerializeField] private RectTransform centerAnchor;
    [SerializeField] private RectTransform rightAnchor;

    [Header("Movimiento de retratos")]
    [Tooltip("Curva de interpolación para el movimiento (0..1). 0: inicio, 1: fin")]
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private void Awake()
    {
        _profiles = (ICharacterProfileService)FindAnyObjectByType<CharacterProfileService>();
        if (_profiles == null)
        {
            var go = new GameObject("_Auto_CharacterProfileService");
            DontDestroyOnLoad(go);
            _profiles = go.AddComponent<CharacterProfileService>();
        }

        _loc = (localizationServiceRef as ILocalizationService) ?? FindAnyObjectByType<CsvLocalizationService>();

        if (!ValidateGraph()) return;
        BuildLookups();
        BuildPortraitPool();
        StartDialogue();

        // --- SANITY CHECK: comprueba que TODOS los profileId del grafo existen en la DB ---
        if (_profiles == null)
        {
            _profiles = FindObjectOfType<CharacterProfileService>();
        }
        if (_profiles != null && graph != null && graph.Nodes != null)
        {
            foreach (var n in graph.Nodes)
            {
                if (string.IsNullOrEmpty(n.profileId))
                {
                    Debug.LogWarning($"[Runner] sanity: node {n.GUID} sin profileId");
                    continue;
                }
                bool ok = _profiles.GetById(n.profileId) != null;
                Debug.Log($"[Runner] sanity: node {n.GUID} id={n.profileId} inDB={ok}");
            }
        }
        else
        {
            Debug.LogWarning("[Runner] sanity: sin _profiles o sin graph");
        }

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

        if (Input.GetKeyDown(advanceKey))
        {
            if (!_animDoneForCurrentNode)
            {
                FastForwardPlacement();
                return;
            }

            if (!_textDoneForCurrentNode)
            {
                if (_isTypewriting)
                    FastForwardTypewriter();
                return;
            }

            if (!_current.isChoiceNode)
            {
                GoNext();
            }
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

    private void StartDialogue()
    {
        _visitedChoiceKeys.Clear();
        DialogueNodeData start = null;

        // 0) Overrides externos correctos (y marcados como inicio)
        if (!string.IsNullOrEmpty(preferredStartGuid) &&
            _nodeByGuid.TryGetValue(preferredStartGuid, out var byGuid) && byGuid.isStart)
        {
            start = byGuid;
        }
        else if (!string.IsNullOrEmpty(preferredStartId))
        {
            start = _nodeByGuid.Values
                .FirstOrDefault(n => n.isStart && string.Equals(n.startId, preferredStartId, StringComparison.OrdinalIgnoreCase));
        }

        // 1) Sin override: primer nodo marcado como inicio
        if (start == null)
        {
            var starts = _nodeByGuid.Values.Where(n => n.isStart).ToList();
            if (starts.Count >= 1) start = starts[0];
        }

        // 2) Sin nodos de inicio: cualquiera sin entradas
        if (start == null)
            start = _nodeByGuid.Values.FirstOrDefault(n => _incomingCount.TryGetValue(n.GUID, out var c) && c == 0);

        // 3) Fallback
        if (start == null)
            start = _nodeByGuid.Values.First();

        SetCurrent(start);
    }

    public void Play(string startId = null, string startGuid = null)
    {
        if (startId != null) preferredStartId = startId;
        if (startGuid != null) preferredStartGuid = startGuid;

        if (!ValidateGraph()) return;
        BuildLookups();
        BuildPortraitPool();
        StartDialogue();
    }

    public void Play(DialogueGraph g, string startId = null, string startGuid = null)
    {
        graph = g;
        Play(startId, startGuid);
    }

    private void SetCurrent(DialogueNodeData node)
    {
        _current = node;

        if (_current != null)
        {
            if (!string.IsNullOrEmpty(_current.eventKey))
            {
                var payload = _current.BuildEventPayload();
                GlobalDialogueEvents.Fire(payload);
            }
            // Nota: DialogueNodeData no define onEnter; línea eliminada.
            // _current.onEnter?.Invoke();
        }

        UpdateDebugLabel();
        _waitingChoice = false;
        HideChoices();

        // Reseteo estado animación/texto
        _animDoneForCurrentNode = false;
        _textDoneForCurrentNode = false;
        _isTypewriting = false;

        if (speakerText) speakerText.text = GetSpeakerName(_current);

        // ¿Cuándo empieza el texto?
        bool startTextNow = _current == null ||
                            _current.textStart == TextStartTiming.Immediate ||
                            _current.textStart == TextStartTiming.OnEnterStart;

        // Sprite del perfil del nodo
        ApplyNodeToUI(_current);

        // Orden visual y efectos (tinte + escala)
        BringPortraitOnTop(_current?.profileId);
        UpdatePortraitHighlight(_current?.profileId);
        UpdatePortraitScale(_current?.profileId);

        // Animación de entrada / salida del retrato
        RunCharacterPlacement(_current, onAnimDone: () =>
        {
            _animDoneForCurrentNode = true;

            if (_current != null && _current.textStart == TextStartTiming.OnEnterComplete)
                DisplayNodeBodyAsync(_current);

            TryShowChoicesWhenReady(_current);
        });

        // Immediate / OnEnterStart → texto ya
        if (startTextNow)
            DisplayNodeBodyAsync(_current);

        // OnEnterMid → mitad de la animación Slide (aprox.)
        if (_current != null && _current.textStart == TextStartTiming.OnEnterMid)
            StartCoroutine(DelayedMidText(_current));
    }

    private System.Collections.IEnumerator DelayedMidText(DialogueNodeData node)
    {
        var hasPool = _portraitRootByProfile.TryGetValue(node.profileId, out var rootRt) && rootRt != null;
        if (!hasPool || node.appearance != AppearanceMode.Slide || node.moveSpeed <= 1f)
        {
            DisplayNodeBodyAsync(node);
            yield break;
        }

        var startPos = ResolveSpot(node.origin, rootRt);
        var endPos = ResolveSpot(node.target, rootRt);
        var dist = Vector3.Distance(startPos, endPos);
        if (dist <= Mathf.Epsilon)
        {
            DisplayNodeBodyAsync(node);
            yield break;
        }

        var duration = dist / Mathf.Max(1f, node.moveSpeed);
        yield return new WaitForSeconds(duration * 0.5f);
        if (_current == node) DisplayNodeBodyAsync(node);
    }

    private void ApplyNodeToUI(DialogueNodeData node)
    {
        bool hasLegacy = portraitImage != null;
        bool hasPool = _portraitByProfile != null && _portraitByProfile.Count > 0;

        if (!hasLegacy && !hasPool) return;
        if (node == null) return;
        if (_profiles == null) return;

        Sprite sprite = null;
        if (!string.IsNullOrEmpty(node.profileId))
        {
            var profile = _profiles.GetById(node.profileId);
            if (profile != null)
            {
                if (!string.IsNullOrEmpty(node.portraitKey))
                    sprite = profile.GetPortraitByKey(node.portraitKey);

                if (sprite == null)
                    sprite = profile.GetPortraitByKey("Default");

                if (sprite == null)
                {
                    var firstKey = profile.GetPortraitKeys().FirstOrDefault();
                    if (!string.IsNullOrEmpty(firstKey))
                        sprite = profile.GetPortraitByKey(firstKey);
                }
            }
        }

        if (!hasPool && hasLegacy)
        {
            portraitImage.sprite = sprite;
            portraitImage.enabled = sprite != null;

            Image activeImg = portraitImage;
            StopSpecialAnimation(activeImg);
            if (node.playSpecialAnimation && node.specialAnimation != null)
                PlaySpecialAnimation(activeImg, node.specialAnimation, node.specialAnimSpeed, node.specialAnimLoop);
            return;
        }

        if (hasPool && !string.IsNullOrEmpty(node.profileId) &&
            _portraitByProfile.TryGetValue(node.profileId, out var img))
        {
            img.sprite = sprite;
            img.enabled = sprite != null;

            StopSpecialAnimation(img);
            if (node.playSpecialAnimation && node.specialAnimation != null)
                PlaySpecialAnimation(img, node.specialAnimation, node.specialAnimSpeed, node.specialAnimLoop);
        }
    }

    private void BringPortraitOnTop(string profileId)
    {
        if (string.IsNullOrEmpty(profileId)) return;
        if (portraitsRoot == null) return;

        if (_portraitByProfile.TryGetValue(profileId, out var img) && img != null)
            img.transform.SetAsLastSibling();
    }

    private void RunCharacterPlacement(DialogueNodeData node, Action onAnimDone)
    {
        if (node == null || string.IsNullOrEmpty(node.profileId) ||
            !_portraitByProfile.TryGetValue(node.profileId, out var img) || img == null)
        {
            onAnimDone?.Invoke();
            return;
        }

        if (!_portraitRootByProfile.TryGetValue(node.profileId, out var rootRt) || rootRt == null)
        {
            onAnimDone?.Invoke();
            return;
        }

        var cg = rootRt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rootRt.gameObject.AddComponent<CanvasGroup>();

        Vector3 startPos = ResolveSpot(node.origin, rootRt);
        rootRt.position = startPos;

        bool exiting = (node.target == Spot.LeftOffscreen || node.target == Spot.RightOffscreen);
        Vector3 endPos = ResolveSpot(node.target, rootRt);

        float from = 1f, to = 1f;
        if (node.useFade)
        {
            if (exiting) { from = node.exitFromOpacity / 100f; to = node.exitToOpacity / 100f; }
            else { from = node.enterFromOpacity / 100f; to = node.enterToOpacity / 100f; }
        }
        cg.alpha = from;

        switch (node.appearance)
        {
            case AppearanceMode.Cut:
                rootRt.position = endPos;
                cg.alpha = to;
                PersistPortraitState(node.profileId, rootRt.position, node.target);
                if (_moveTweens.ContainsKey(rootRt)) _moveTweens.Remove(rootRt);
                onAnimDone?.Invoke();
                break;

            case AppearanceMode.Fade:
                {
                    // Cancelar si había uno en curso
                    if (_moveTweens.TryGetValue(rootRt, out var runningFade) && runningFade != null)
                        StopCoroutine(runningFade);

                    var coFade = StartCoroutine(SoloFade(rootRt, cg, to, onAnimDone, node.profileId, node.target));
                    _moveTweens[rootRt] = coFade;
                    break;
                }

            case AppearanceMode.Slide:
                {
                    if (node.moveSpeed <= 1f)
                    {
                        rootRt.position = endPos;
                        cg.alpha = to;
                        PersistPortraitState(node.profileId, rootRt.position, node.target);
                        if (_moveTweens.ContainsKey(rootRt)) _moveTweens.Remove(rootRt);
                        onAnimDone?.Invoke();
                    }
                    else
                    {
                        // Cancelar si había uno en curso
                        if (_moveTweens.TryGetValue(rootRt, out var runningSlide) && runningSlide != null)
                            StopCoroutine(runningSlide);

                        var coSlide = StartCoroutine(SlideAndFade(rootRt, cg, endPos, to, node.moveSpeed, onAnimDone, node.profileId, node.target));
                        _moveTweens[rootRt] = coSlide;
                    }
                    break;
                }

            case AppearanceMode.None:
            default:
                PersistPortraitState(node.profileId, rootRt.position, node.target);
                if (_moveTweens.ContainsKey(rootRt)) _moveTweens.Remove(rootRt);
                onAnimDone?.Invoke();
                break;
        }
    }

    private void PersistPortraitState(string profileId, Vector3 pos, Spot target)
    {
        _portraitStateByProfile[profileId] = new PortraitState
        {
            logical = ResolveLogical(target),
            screenPos = pos
        };
    }

    //private System.Collections.IEnumerator SoloFade(CanvasGroup cg, float endAlpha, Action onDone, string profileId, Vector3 pos, Spot target)
    //{
    //    float startAlpha = cg.alpha;
    //    float t = 0f;
    //    while (t < 1f)
    //    {
    //        t += Time.deltaTime;
    //        cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
    //        yield return null;
    //    }
    //    PersistPortraitState(profileId, pos, target);
    //    onDone?.Invoke();
    //}

    private System.Collections.IEnumerator SoloFade(RectTransform rootRt, CanvasGroup cg, float endAlpha, Action onDone, string profileId, Spot target)
    {
        float startAlpha = cg.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        if (rootRt != null)
        {
            // Persistimos la posición final y limpiamos el registro del tween
            PersistPortraitState(profileId, rootRt.position, target);
            if (_moveTweens.ContainsKey(rootRt)) _moveTweens.Remove(rootRt);
        }

        onDone?.Invoke();
    }


    private System.Collections.IEnumerator SlideAndFade(
        RectTransform rt, CanvasGroup cg, Vector3 endPos, float endAlpha, float speed, Action onDone,
        string profileId, Spot target)
    {
        Vector3 startPos = rt.position;
        float startAlpha = cg.alpha;

        float totalDist = Vector3.Distance(startPos, endPos);
        if (totalDist <= Mathf.Epsilon)
        {
            float t0 = 0f;
            while (t0 < 1f)
            {
                t0 += Time.deltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t0);
                yield return null;
            }
            PersistPortraitState(profileId, rt.position, target);
            if (_moveTweens.ContainsKey(rt)) _moveTweens.Remove(rt);
            onDone?.Invoke();
            yield break;
        }

        float duration = totalDist / speed;
        float elapsed = 0f;
        AnimationCurve curve = moveCurve != null ? moveCurve : AnimationCurve.Linear(0f, 0f, 1f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(elapsed / duration);
            float easedT = Mathf.Clamp01(curve.Evaluate(rawT));

            rt.position = Vector3.LerpUnclamped(startPos, endPos, easedT);
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, rawT);

            yield return null;
        }

        rt.position = endPos;
        cg.alpha = endAlpha;
        PersistPortraitState(profileId, rt.position, target);
        if (_moveTweens.ContainsKey(rt)) _moveTweens.Remove(rt);
        onDone?.Invoke();
    }

    private void BuildPortraitPool()
    {
        if (portraitsRoot == null || portraitPrefab == null) return;

        // Parar y limpiar tweens de colocación pendientes
        foreach (var kv in _moveTweens)
            if (kv.Value != null) StopCoroutine(kv.Value);
        _moveTweens.Clear();

        // Destruir animaciones especiales activas para estas imágenes antes de borrarlas
        foreach (var kv in _specialAnimGraphs)
        {
            if (kv.Value.IsValid()) kv.Value.Destroy();
        }
        _specialAnimGraphs.Clear();

        foreach (var kv in _portraitByProfile)
            if (kv.Value) Destroy(kv.Value.gameObject);
        _portraitByProfile.Clear();

        foreach (var kv in _portraitRootByProfile)
            if (kv.Value) Destroy(kv.Value.gameObject);
        _portraitRootByProfile.Clear();

        var uniqueProfiles = graph.Nodes
            .Where(n => n != null && !string.IsNullOrEmpty(n.profileId))
            .Select(n => n.profileId)
            .Distinct();

        foreach (var pid in uniqueProfiles)
        {
            var rootGo = new GameObject($"PortraitRoot_{pid}", typeof(RectTransform), typeof(CanvasGroup));
            var rootRt = (RectTransform)rootGo.transform;
            rootRt.SetParent(portraitsRoot, worldPositionStays: false);

            if (centerAnchor != null) rootRt.position = centerAnchor.position;
            else
            {
                rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0.5f);
                rootRt.anchoredPosition = Vector2.zero;
            }
            rootRt.localScale = Vector3.one;

            var cg = rootGo.GetComponent<CanvasGroup>();
            cg.alpha = 0f;

            var img = Instantiate(portraitPrefab, rootRt);
            img.gameObject.name = $"Portrait_{pid}";
            img.gameObject.SetActive(true);
            img.enabled = false;

            var imgRt = img.rectTransform;
            imgRt.anchorMin = imgRt.anchorMax = new Vector2(0.5f, 0.5f);
            imgRt.pivot = new Vector2(0.5f, 0.5f);
            imgRt.anchoredPosition = Vector2.zero;
            imgRt.localPosition = Vector3.zero;
            imgRt.localRotation = Quaternion.identity;
            imgRt.localScale = Vector3.one;

            _portraitByProfile[pid] = img;
            _portraitRootByProfile[pid] = rootRt;
            _portraitStateByProfile[pid] = new PortraitState { logical = LogicalPos.None, screenPos = rootRt.position };
        }

        if (portraitImage) portraitImage.enabled = false; // oculta legacy por defecto
    }

    private void UpdatePortraitScale(string currentProfileId)
    {
        if (_portraitByProfile == null || _portraitByProfile.Count == 0)
        {
            if (portraitImage != null)
                StartScaleTween(portraitImage.rectTransform, speakingScale);
            return;
        }

        bool hasSpeaker = !string.IsNullOrEmpty(currentProfileId);
        foreach (var kv in _portraitByProfile)
        {
            var img = kv.Value;
            if (!img) continue;

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
        if (!rt) return;

        if (_scaleTweens.TryGetValue(rt, out var running) && running != null)
            StopCoroutine(running);

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
        while (t < duration && rt != null)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float k = (curve != null) ? curve.Evaluate(u) : u;
            rt.localScale = Vector3.LerpUnclamped(from, to, k);
            yield return null;
        }
        if (rt != null) rt.localScale = to;
        if (rt != null) _scaleTweens.Remove(rt);
    }

    private void StopSpecialAnimation(Image img)
    {
        if (!img) return;
        if (_specialAnimGraphs.TryGetValue(img, out var g))
        {
            if (g.IsValid()) g.Destroy();
            _specialAnimGraphs.Remove(img);
        }
    }

    private void PlaySpecialAnimation(Image img, AnimationClip clip, float speed = 1f, bool loop = true)
    {
        if (!img || !clip) return;

        var animator = img.GetComponent<Animator>();
        if (animator == null) animator = img.gameObject.AddComponent<Animator>();

        StopSpecialAnimation(img);

        var graph = PlayableGraph.Create($"DG_SpecialAnim_{img.name}");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        var playableOutput = AnimationPlayableOutput.Create(graph, "AnimOutput", animator);
        var clipPlayable = AnimationClipPlayable.Create(graph, clip);
        clipPlayable.SetSpeed(Mathf.Approximately(speed, 0f) ? 0f : speed);

        playableOutput.SetSourcePlayable(clipPlayable);
        graph.Play();
        _specialAnimGraphs[img] = graph;

        if (!loop && clip.length > 0f && speed > 0f)
            StartCoroutine(StopGraphWhenDone(graph, (float)(clip.length / speed)));
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

    private void UpdatePortraitHighlight(string currentProfileId)
    {
        if (_portraitByProfile == null || _portraitByProfile.Count == 0)
        {
            if (portraitImage != null) portraitImage.color = speakingTint;
            return;
        }

        bool hasSpeaker = !string.IsNullOrEmpty(currentProfileId);
        foreach (var kv in _portraitByProfile)
        {
            var img = kv.Value;
            if (!img) continue;

            if (!dimNonSpeaking || !hasSpeaker) { img.color = speakingTint; continue; }
            img.color = (kv.Key == currentProfileId) ? speakingTint : nonSpeakingTint;
        }
    }

    private async void DisplayNodeBodyAsync(DialogueNodeData node)
    {
        if (bodyText == null) return;

        string nodeText = ResolveBodyText(node);
        _currentNodeFullText = nodeText;

        if (node == null || !node.useTypewriter)
        {
            bodyText.SetText(nodeText);
            _textDoneForCurrentNode = true;
            TryShowChoicesWhenReady(node);
            return;
        }

        _twCts?.Cancel();
        _twCts?.Dispose();
        _twCts = new CancellationTokenSource();

        //// Parar tweens y graphs si el objeto se destruye en medio
        //foreach (var kv in _moveTweens)
        //    if (kv.Value != null) StopCoroutine(kv.Value);
        //_moveTweens.Clear();

        //foreach (var kv in _scaleTweens)
        //    if (kv.Value != null) StopCoroutine(kv.Value);
        //_scaleTweens.Clear();

        //foreach (var kv in _specialAnimGraphs)
        //{
        //    if (kv.Value.IsValid()) kv.Value.Destroy();
        //}
        //_specialAnimGraphs.Clear();

        var p = ScriptableObject.CreateInstance<TypewriterProfile>();
        _isTypewriting = true;
        p.secondsPerChar = node.tw.secondsPerChar;
        p.globalSpeed = node.tw.globalSpeed;
        p.respectRichText = node.tw.respectRichText;
        p.minimalWhitespaceDelay = node.tw.minimalWhitespaceDelay;
        p.commaPct = node.tw.commaPct;
        p.periodPct = node.tw.periodPct;
        p.ellipsisPct = node.tw.ellipsisPct;

        try
        {
            await _typewriter.RunAsync(nodeText, p, bodyText, null, _twCts.Token);
            _isTypewriting = false;
            _textDoneForCurrentNode = true;
            TryShowChoicesWhenReady(node);
        }
        catch (OperationCanceledException)
        {
            // Si hemos cancelado porque hicimos fast-forward, el método FastForwardTypewriter()
            // ya dejó el texto y flags en buen estado. Solo aseguramos el flag:
            _isTypewriting = false;
        }
        finally
        {
            if (p != null) Destroy(p);
        }
    }

    // --- Afinidad: comprobación centralizada ---
    private bool IsChoiceAllowedByAffinity(DialogueNodeData.ChoiceData choice)
    {
        if (choice == null) return true;
        if (!choice.requiresAffinity) return true;

        // Obtenemos el valor actual desde ParamService por clave
        var key = choice.affinityKey ?? string.Empty;
        float current = ParamService.GetFloat(key, 0f); // si no existe la clave, 0 por defecto

        // Comparación directa o invertida
        if (!choice.invertRequirement)
            return current >= choice.requiredAffinity;   // normal: debe ser >= requerido
        else
            return current < choice.requiredAffinity;    // invertida: debe ser < requerido
    }

    private string ResolveBodyText(DialogueNodeData node)
    {
        if (node == null) return string.Empty;
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

                // --- FILTROS DE REQUISITOS ---
                bool allowed = IsChoiceAllowedByAffinity(choice) && IsChoiceAllowedByProgression(choice);
                if (!allowed)
                {
                    btn.gameObject.SetActive(false);
                    continue;
                }

                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.text = string.IsNullOrEmpty(choice.choiceText) ? $"Opción {i + 1}" : choice.choiceText;

                var port = string.IsNullOrEmpty(choice.portName) ? $"choice_{i}" : choice.portName;

                var visited = _visitedChoiceKeys.Contains(MakeChoiceKey(node.GUID, port));
                var bg = btn.image;
                if (bg) bg.color = visited ? visitedChoiceColor : normalChoiceColor;

                btn.onClick.AddListener(() => OnChoiceSelected(node.GUID, port));
                btn.gameObject.SetActive(true);
            }
            else btn.gameObject.SetActive(false);
        }
    }

    // --- Progreso: comprobación centralizada ---
    private bool IsChoiceAllowedByProgression(DialogueNodeData.ChoiceData choice)
    {
        if (choice == null) return true;
        if (!choice.requiresProgress) return true;

        // Debe haber un nombre de método
        var method = choice.progressMethod ?? string.Empty;
        if (string.IsNullOrWhiteSpace(method))
        {
            Debug.LogWarning("[DialogueRunner] Opción requiere progreso pero no hay método definido.");
            return false;
        }

        // Resolver el argumento según el tipo elegido
        object arg = null;
        switch (choice.progressArgType)
        {
            case ProgressArgType.None: arg = null; break;
            case ProgressArgType.Int: arg = choice.progressArgInt; break;
            case ProgressArgType.Float: arg = choice.progressArgFloat; break;
            case ProgressArgType.String: arg = choice.progressArgString ?? string.Empty; break;
            default: arg = null; break;
        }

        // Intentar invocar el método registrado
        if (ProgressConditionInvoker.TryInvoke(method, arg, out bool result))
        {
            Debug.LogWarning("QUE ERESH: " + result);
            return result;
        }

        // Si no hay registro, lo consideramos NO permitido y avisamos
        Debug.LogWarning($"[DialogueRunner] Método de progreso no registrado o inválido: '{method}'.");
        return false;
    }

    private void HideChoices()
    {
        foreach (var b in choiceButtons)
            if (b) b.gameObject.SetActive(false);
    }

    private void OnChoiceSelected(string fromGuid, string fromPort)
    {
        _waitingChoice = false;
        _visitedChoiceKeys.Add(MakeChoiceKey(fromGuid, fromPort));
        var key = (fromGuid, fromPort);

        if (_edgeLookup.TryGetValue(key, out var toGuid) && _nodeByGuid.TryGetValue(toGuid, out var toNode))
            SetCurrent(toNode);
        else
            EndDialogue();
    }

    private static string MakeChoiceKey(string guid, string port) => $"{guid}::{port}";

    private void GoNext()
    {
        if (_current == null) return;

        var key = (_current.GUID, "Next");
        if (_edgeLookup.TryGetValue(key, out var toGuid) && _nodeByGuid.TryGetValue(toGuid, out var toNode))
        {
            SetCurrent(toNode); return;
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

        // Cancelar typewriter en curso (si lo hay)
        _twCts?.Cancel();
        _twCts?.Dispose();
        _twCts = null;

        foreach (var kv in _moveTweens) if (kv.Value != null) StopCoroutine(kv.Value);
        _moveTweens.Clear();

        foreach (var kv in _scaleTweens) if (kv.Value != null) StopCoroutine(kv.Value);
        _scaleTweens.Clear();

        foreach (var kv in _specialAnimGraphs) if (kv.Value.IsValid()) kv.Value.Destroy();
        _specialAnimGraphs.Clear();

        // Destruir animaciones especiales (por si hay loops vivos)
        foreach (var kv in _specialAnimGraphs)
        {
            if (kv.Value.IsValid()) kv.Value.Destroy();
        }
        _specialAnimGraphs.Clear();

        // Parar tweens activos (colocación y escala) y limpiar registros
        foreach (var kv in _moveTweens)
            if (kv.Value != null) StopCoroutine(kv.Value);
        _moveTweens.Clear();

        foreach (var kv in _scaleTweens)
            if (kv.Value != null) StopCoroutine(kv.Value);
        _scaleTweens.Clear();

        if (speakerText) speakerText.text = "";
        if (bodyText) bodyText.text = "<i>(Fin del diálogo)</i>";

        if (portraitImage)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }

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
        if (debugNodeText == null) return;
        debugNodeText.gameObject.SetActive(debugMode);
        if (!debugMode) return;

        var id = _current != null ? _current.GUID : "(end/null)";
        debugNodeText.text = $"Node GUID: {id}";
    }

    public string CurrentNodeGuid => _current != null ? _current.GUID : null;

    // --- Helpers de compatibilidad de nombres ---
    private static string GetNodeText(object node)
        => TryGetStringField(node, "lineText", "text", "dialogueText", "dialogText", "content", "body");

    private static string GetSpeakerName(object node)
        => TryGetStringField(node, "speakerName", "speaker", "character", "name");

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

    private Vector3 ResolveSpot(Spot spot, RectTransform rt)
    {
        switch (spot)
        {
            case Spot.Left: return GetAnchorPos(leftAnchor, rt.position);
            case Spot.CenterLeft: return GetAnchorPos(leftAnchor ? leftAnchor : centerAnchor, rt.position);
            case Spot.Center: return GetAnchorPos(centerAnchor, rt.position);
            case Spot.CenterRight: return GetAnchorPos(rightAnchor ? rightAnchor : centerAnchor, rt.position);
            case Spot.Right: return GetAnchorPos(rightAnchor, rt.position);
            case Spot.LeftOffscreen: return OffscreenFromAnchor(GetAnchorPos(leftAnchor, rt.position), true);
            case Spot.RightOffscreen: return OffscreenFromAnchor(GetAnchorPos(rightAnchor, rt.position), false);
            default: return rt.position;
        }
    }

    private Vector3 OffscreenFromAnchor(Vector3 near, bool toLeft)
    {
        var p = near;
        p.x += toLeft ? -Screen.width * 0.6f : Screen.width * 0.6f;
        return p;
    }

    private LogicalPos ResolveLogical(Spot s)
    {
        switch (s)
        {
            case Spot.Left: return LogicalPos.Left;
            case Spot.Center:
            case Spot.CenterLeft:
            case Spot.CenterRight: return LogicalPos.Center;
            case Spot.Right: return LogicalPos.Right;
            case Spot.LeftOffscreen: return LogicalPos.OffLeft;
            case Spot.RightOffscreen: return LogicalPos.OffRight;
            default: return LogicalPos.None;
        }
    }
    private void TryShowChoicesWhenReady(DialogueNodeData node)
    {
        if (node == null || !node.isChoiceNode) return;

        // Mostrar opciones únicamente cuando han acabado animación y texto
        if (_animDoneForCurrentNode && _textDoneForCurrentNode)
            ShowChoices(node);
    }

    // Completa inmediatamente el texto del nodo actual si el typewriter está en curso
    private void FastForwardTypewriter()
    {
        if (!_isTypewriting) return;

        // Cancelamos la tarea asíncrona actual del typewriter
        _twCts?.Cancel();
        _twCts?.Dispose();
        _twCts = null;

        // Pintamos el texto completo y establecemos estado "texto terminado"
        if (bodyText != null)
            bodyText.SetText(_currentNodeFullText ?? string.Empty);

        _isTypewriting = false;
        _textDoneForCurrentNode = true;

        // Si este nodo es de elección, puede que ya estén listas las opciones
        TryShowChoicesWhenReady(_current);
    }

    // Completa inmediatamente la animación básica de colocación del portrait del nodo actual
    private void FastForwardPlacement()
    {
        if (_animDoneForCurrentNode) return;
        if (_current == null || string.IsNullOrEmpty(_current.profileId)) { _animDoneForCurrentNode = true; return; }

        if (!_portraitRootByProfile.TryGetValue(_current.profileId, out var rootRt) || rootRt == null)
        {
            _animDoneForCurrentNode = true;
            TryShowChoicesWhenReady(_current);
            return;
        }

        // Parar tween en curso si lo hubiera
        if (_moveTweens.TryGetValue(rootRt, out var running) && running != null)
        {
            StopCoroutine(running);
            _moveTweens.Remove(rootRt);
        }

        var cg = rootRt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rootRt.gameObject.AddComponent<CanvasGroup>();

        bool exiting = (_current.target == Spot.LeftOffscreen || _current.target == Spot.RightOffscreen);
        float toAlpha = 1f;
        if (_current.useFade)
        {
            toAlpha = exiting ? _current.exitToOpacity / 100f : _current.enterToOpacity / 100f;
        }

        // Estado final inmediato
        Vector3 endPos = ResolveSpot(_current.target, rootRt);
        rootRt.position = endPos;
        cg.alpha = toAlpha;

        PersistPortraitState(_current.profileId, rootRt.position, _current.target);
        _animDoneForCurrentNode = true;

        // Si el texto estaba configurado para arrancar al completar la entrada, lánzalo
        if (_current.textStart == TextStartTiming.OnEnterComplete)
            DisplayNodeBodyAsync(_current);

        TryShowChoicesWhenReady(_current);
    }
}

// ============================================================================
// Registro simple para condiciones de progreso (bool) invocables por nombre.
// En tu bootstrap del juego registra así:
//   ProgressConditionInvoker.Register("HasClearedDungeon", (arg) => MyGameProg.HasClearedDungeon((string)arg));
//   ProgressConditionInvoker.Register("StoryReached", (arg) => MyGameProg.StoryReached((int)arg));
// Devuelve true/false; si no se encuentra, TryInvoke => false + result=false.
// ============================================================================
static class ProgressConditionInvoker
{
    private static readonly Dictionary<string, Func<object, bool>> _registry = new();

    public static void Register(string methodName, Func<object, bool> fn)
    {
        if (string.IsNullOrWhiteSpace(methodName) || fn == null) return;
        _registry[methodName] = fn;
    }

    public static void Unregister(string methodName)
    {
        if (string.IsNullOrWhiteSpace(methodName)) return;
        _registry.Remove(methodName);
    }

    public static bool TryInvoke(string methodName, object arg, out bool result)
    {
        result = false;
        if (string.IsNullOrWhiteSpace(methodName)) return false;
        if (_registry.TryGetValue(methodName, out var fn))
        {
            try
            {
                result = fn.Invoke(arg);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProgressConditionInvoker] Excepción invocando '{methodName}': {ex.Message}");
                return false;
            }
        }
        return false;
    }
}

