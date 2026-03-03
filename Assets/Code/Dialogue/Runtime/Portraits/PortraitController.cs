using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

// =============================================================
// PortraitController — implementa IPortraitController y IPortraitPlacementMilestones
// Gestiona: pool de retratos por perfil, orden visual, tinte/escala, y
// animaciones de colocación (Cut/Fade/Slide) + animaciones especiales (Playables).
// =============================================================

[DisallowMultipleComponent]
public sealed class PortraitController : MonoBehaviour, IPortraitController, IPortraitPlacementMilestones
{
    #region Inspector

    [Header("Anchors de posición")]
    [SerializeField] private RectTransform leftAnchor;
    [SerializeField] private RectTransform centerLeftAnchor;
    [SerializeField] private RectTransform centerAnchor;
    [SerializeField] private RectTransform centerRightAnchor;
    [SerializeField] private RectTransform rightAnchor;

    [Header("Pool de retratos")]
    [SerializeField] private RectTransform portraitsRoot;
    [SerializeField] private Image portraitPrefab;

    [Header("Tinte / escalado")]
    [SerializeField] private bool dimNonSpeaking = true;
    [SerializeField] private Color speakingTint    = Color.white;
    [SerializeField] private Color nonSpeakingTint = new(1f, 1f, 1f, 0.50f);
    [SerializeField] private bool scaleNonSpeaking = true;
    [SerializeField] private Vector3 speakingScale    = Vector3.one;
    [SerializeField] private Vector3 nonSpeakingScale = new(0.95f, 0.95f, 0.95f);
    [SerializeField, Min(0f)] private float scaleTweenDuration = 0.15f;
    [SerializeField] private AnimationCurve scaleTweenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Movimiento de colocación")]
    [SerializeField] private AnimationCurve moveCurve = null;

    [Header("Animación especial")]
    [SerializeField] private bool resetSpecialsWithStaticPoseOnNodeChange = true;
    [SerializeField] private AnimationClip specialStaticPoseClip;

    [Header("Perfiles")]
    [SerializeField] private CharacterProfileDatabase profileDatabase;

    #endregion

    #region Estado interno

    private DialogueGraph _graph;

    private readonly Dictionary<string, Image>         _portraitByProfile = new();
    private readonly Dictionary<string, RectTransform> _rootByProfile     = new();
    private readonly Dictionary<Image, PlayableGraph>  _specialGraphs     = new();
    private readonly Dictionary<string, (AnimationClip clip, float speed, bool loop)> _pendingSpecialByProfile = new();
    private readonly Dictionary<RectTransform, Coroutine> _placementCo = new();
    private readonly Dictionary<RectTransform, Coroutine> _scaleCo     = new();

    private readonly struct PortraitState
    {
        public readonly Vector3 screenPos;
        public PortraitState(Vector3 p) { screenPos = p; }
    }

    private struct Pose
    {
        public Vector3 pos;
        public float   alpha;
        public Vector3 scale;
        public Pose(Vector3 p, float a, Vector3 s) { pos = p; alpha = a; scale = s; }
    }

    private readonly Dictionary<string, PortraitState> _stateByProfile = new();

    #endregion

    #region IPortraitController

    public void Init(DialogueGraph graph)
    {
        _graph = graph;
        RebuildPool();
    }

    public async Task ApplyAsync(PortraitModule module)
    {
        if (module == null || string.IsNullOrEmpty(module.profileId)
            || !_rootByProfile.TryGetValue(module.profileId, out var rootRt))
        {
            if (resetSpecialsWithStaticPoseOnNodeChange) ResetSpecialsToStaticPose();
            return;
        }

        if (resetSpecialsWithStaticPoseOnNodeChange) ResetSpecialsToStaticPose();

        var img = _portraitByProfile[module.profileId];
        SetSpriteForModule(img, module);

        BringOnTop(module.profileId);
        UpdateTint(module.profileId);
        UpdateScale(module.profileId);

        _pendingSpecialByProfile.Remove(module.profileId);

        if (module.playSpecialAnimation && module.specialAnimation)
        {
            switch (module.specialStart)
            {
                case SpecialStartTiming.Immediate:
                case SpecialStartTiming.WithPlacementStart:
                    PlaySpecial(img, module.specialAnimation, module.specialAnimSpeed, module.specialAnimLoop);
                    break;

                case SpecialStartTiming.WithTextStart:
                    // En el sistema modular, el módulo de texto sigue al de retrato.
                    // Tratamos WithTextStart como WithPlacementComplete para mantener
                    // un comportamiento coherente con la secuencia de módulos.
                    StopSpecial(img);
                    break;

                case SpecialStartTiming.WithPlacementComplete:
                    StopSpecial(img);
                    break;
            }
        }
        else
        {
            StopSpecial(img);
        }

        await RunPlacementAsync(module, rootRt);

        if (module.playSpecialAnimation && module.specialAnimation &&
            (module.specialStart == SpecialStartTiming.WithPlacementComplete ||
             module.specialStart == SpecialStartTiming.WithTextStart))
        {
            PlaySpecial(img, module.specialAnimation, module.specialAnimSpeed, module.specialAnimLoop);
        }
    }

    public void ResetAll()
    {
        foreach (var kv in _placementCo) if (kv.Value != null) StopCoroutine(kv.Value);
        _placementCo.Clear();
        foreach (var kv in _scaleCo) if (kv.Value != null) StopCoroutine(kv.Value);
        _scaleCo.Clear();
        foreach (var kv in _specialGraphs) if (kv.Value.IsValid()) kv.Value.Destroy();
        _specialGraphs.Clear();
        _pendingSpecialByProfile.Clear();

        foreach (var kv in _portraitByProfile)
        {
            if (!kv.Value) continue;
            kv.Value.enabled = false;
            var cg = kv.Value.GetComponentInParent<CanvasGroup>();
            if (cg) cg.alpha = 0f;
        }
    }

    #endregion

    #region Pool & Sprite

    private void RebuildPool()
    {
        foreach (var kv in _placementCo) if (kv.Value != null) StopCoroutine(kv.Value);
        _placementCo.Clear();
        foreach (var kv in _scaleCo) if (kv.Value != null) StopCoroutine(kv.Value);
        _scaleCo.Clear();
        foreach (var kv in _specialGraphs) if (kv.Value.IsValid()) kv.Value.Destroy();
        _specialGraphs.Clear();
        foreach (var kv in _portraitByProfile) if (kv.Value) Destroy(kv.Value.gameObject);
        _portraitByProfile.Clear();
        foreach (var kv in _rootByProfile) if (kv.Value) Destroy(kv.Value.gameObject);
        _rootByProfile.Clear();

        if (_graph == null || portraitsRoot == null || portraitPrefab == null) return;

        // Escanear PortraitModules en todos los nodos del grafo
        var uniqueProfiles = _graph.Nodes
            .Where(n => n?.modules != null)
            .SelectMany(n => n.modules.OfType<PortraitModule>())
            .Where(m => !string.IsNullOrEmpty(m.profileId))
            .Select(m => m.profileId)
            .Distinct();

        foreach (var pid in uniqueProfiles)
        {
            var rootGo = new GameObject($"PortraitRoot_{pid}", typeof(RectTransform), typeof(CanvasGroup));
            var rootRt = (RectTransform)rootGo.transform;
            rootRt.SetParent(portraitsRoot, false);

            if (centerAnchor) rootRt.position = centerAnchor.position;
            else { rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0.5f); rootRt.anchoredPosition = Vector2.zero; }
            rootRt.localScale = Vector3.one;

            var cg = rootGo.GetComponent<CanvasGroup>();
            cg.alpha = 0f;

            var img = Instantiate(portraitPrefab, rootRt);
            img.gameObject.name = $"Portrait_{pid}";
            img.enabled = false;

            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            _portraitByProfile[pid] = img;
            _rootByProfile[pid]     = rootRt;
            _stateByProfile[pid]    = new PortraitState(rootRt.position);
        }
    }

    private void SetSpriteForModule(Image img, PortraitModule module)
    {
        if (!img || module == null || profileDatabase == null) return;

        Sprite sprite = null;
        if (!string.IsNullOrEmpty(module.profileId))
        {
            var profile = profileDatabase.FindById(module.profileId);
            if (profile != null)
                sprite = profile.GetSprite(module.portraitKey);
        }

        img.sprite  = sprite;
        img.enabled = sprite != null;
    }

    #endregion

    #region Orden, tinte y escala

    private void BringOnTop(string profileId)
    {
        if (string.IsNullOrEmpty(profileId)) return;
        if (_portraitByProfile.TryGetValue(profileId, out var img) && img)
            img.transform.SetAsLastSibling();
    }

    private void UpdateTint(string currentProfileId)
    {
        if (_portraitByProfile.Count == 0) return;
        bool hasSpeaker = !string.IsNullOrEmpty(currentProfileId);
        foreach (var kv in _portraitByProfile)
        {
            var img = kv.Value; if (!img) continue;
            if (!dimNonSpeaking || !hasSpeaker) { img.color = speakingTint; continue; }
            img.color = (kv.Key == currentProfileId) ? speakingTint : nonSpeakingTint;
        }
    }

    private void UpdateScale(string currentProfileId)
    {
        if (_portraitByProfile.Count == 0) return;
        bool hasSpeaker = !string.IsNullOrEmpty(currentProfileId);

        foreach (var kv in _portraitByProfile)
        {
            var img = kv.Value; if (!img) continue;
            var rt  = img.rectTransform;
            var target = (!scaleNonSpeaking || !hasSpeaker)
                ? speakingScale
                : (kv.Key == currentProfileId ? speakingScale : nonSpeakingScale);
            StartScaleTween(rt, target);
        }
    }

    private void StartScaleTween(RectTransform rt, Vector3 targetScale)
    {
        if (!rt) return;
        if (_scaleCo.TryGetValue(rt, out var running) && running != null)
            StopCoroutine(running);

        if (scaleTweenDuration <= 0f)
        {
            rt.localScale = targetScale;
            _scaleCo.Remove(rt);
            return;
        }
        _scaleCo[rt] = StartCoroutine(CoScale(rt, targetScale, scaleTweenDuration, scaleTweenCurve));
    }

    private IEnumerator CoScale(RectTransform rt, Vector3 to, float duration, AnimationCurve curve)
    {
        Vector3 from = rt.localScale;
        float t = 0f;
        while (t < duration && rt)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float k = (curve != null) ? curve.Evaluate(u) : u;
            rt.localScale = Vector3.LerpUnclamped(from, to, k);
            yield return null;
        }
        if (rt) rt.localScale = to;
        _scaleCo.Remove(rt);
    }

    #endregion

    #region Colocación (Cut/Fade/Slide)

    private async Task RunPlacementAsync(PortraitModule module, RectTransform rootRt, System.Action<float> onProgress = null)
    {
        if (!rootRt) return;

        if (_placementCo.TryGetValue(rootRt, out var running) && running != null)
        {
            StopCoroutine(running);
            _placementCo.Remove(rootRt);
        }

        var cg = rootRt.GetComponent<CanvasGroup>();
        if (!cg) cg = rootRt.gameObject.AddComponent<CanvasGroup>();

        var from = ComputeStartPose(module, rootRt);
        var to   = ComputeEndPose(module, rootRt);

        rootRt.position = from.pos;
        cg.alpha        = from.alpha;

        switch (module.appearance)
        {
            case AppearanceMode.Cut:
                rootRt.position = to.pos;
                cg.alpha        = to.alpha;
                _stateByProfile[module.profileId] = new PortraitState(rootRt.position);
                return;

            case AppearanceMode.Slide:
                if (module.moveSpeed <= 1f)
                {
                    rootRt.position = to.pos;
                    cg.alpha        = to.alpha;
                    _stateByProfile[module.profileId] = new PortraitState(rootRt.position);
                    return;
                }
                await RunAsTask(CoPlace(rootRt, cg, from, to, module.moveSpeed, onProgress));
                _stateByProfile[module.profileId] = new PortraitState(rootRt.position);
                return;

            default:
                return;
        }
    }

    private IEnumerator CoPlace(RectTransform rt, CanvasGroup cg, Pose from, Pose to, float speed, System.Action<float> onProgress)
    {
        Vector3 startPos   = from.pos;
        float   startAlpha = from.alpha;

        float dist = Vector3.Distance(startPos, to.pos);
        if (dist <= Mathf.Epsilon && Mathf.Abs(startAlpha - to.alpha) <= Mathf.Epsilon)
            yield break;

        float duration = (dist <= Mathf.Epsilon)
            ? (1f / Mathf.Max(0.0001f, speed))
            : (dist / Mathf.Max(0.0001f, speed));

        float elapsed = 0f;
        var curve = (moveCurve != null) ? moveCurve : AnimationCurve.Linear(0f, 0f, 1f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float rawT   = Mathf.Clamp01(elapsed / duration);
            float easedT = Mathf.Clamp01(curve.Evaluate(rawT));

            rt.position = Vector3.LerpUnclamped(startPos, to.pos, easedT);
            if (cg) cg.alpha = Mathf.Lerp(startAlpha, to.alpha, rawT);

            onProgress?.Invoke(rawT);
            yield return null;
        }

        rt.position = to.pos;
        if (cg) cg.alpha = to.alpha;
    }

    private Task RunAsTask(IEnumerator co)
    {
        var tcs = new TaskCompletionSource<bool>();
        StartCoroutine(Wrap(co, tcs));
        return tcs.Task;
    }

    private IEnumerator Wrap(IEnumerator co, TaskCompletionSource<bool> tcs)
    {
        yield return co;
        tcs.TrySetResult(true);
    }

    #endregion

    #region Special Animations (Playables)

    private void PlaySpecial(Image img, AnimationClip clip, float speed, bool loop)
    {
        if (!img || !clip) return;
        StopSpecial(img);

        var animator = img.GetComponent<Animator>();
        if (!animator) animator = img.gameObject.AddComponent<Animator>();

        var graph = PlayableGraph.Create($"DG_SpecialAnim_{img.name}");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        var output      = AnimationPlayableOutput.Create(graph, "AnimOutput", animator);
        var clipPlayable = AnimationClipPlayable.Create(graph, clip);
        clipPlayable.SetSpeed(Mathf.Approximately(speed, 0f) ? 0f : speed);
        clipPlayable.SetApplyFootIK(false);

        output.SetSourcePlayable(clipPlayable);
        graph.Play();
        _specialGraphs[img] = graph;

        if (!loop && clip.length > 0f && speed > 0f)
            StartCoroutine(StopGraphWhenDone(graph, (float)(clip.length / speed)));
    }

    private IEnumerator StopGraphWhenDone(PlayableGraph g, float delay)
    {
        float t = 0f;
        while (t < delay && g.IsValid()) { t += Time.deltaTime; yield return null; }
        if (g.IsValid()) g.Destroy();
    }

    private void StopSpecial(Image img)
    {
        if (!img) return;
        if (_specialGraphs.TryGetValue(img, out var g))
        {
            if (g.IsValid()) g.Destroy();
            _specialGraphs.Remove(img);
        }
    }

    private void ResetSpecialsToStaticPose()
    {
        if (!resetSpecialsWithStaticPoseOnNodeChange || !specialStaticPoseClip) return;
        if (_specialGraphs.Count == 0) return;

        var imgs = _specialGraphs.Keys.ToList();
        foreach (var img in imgs)
        {
            StopSpecial(img);
            var go = img ? img.gameObject : null;
            if (go) specialStaticPoseClip.SampleAnimation(go, 0f);
        }
    }

    #endregion

    #region Spots & Utils

    private Vector3 GetAnchorPos(RectTransform anchor, Vector3 fallback)
        => anchor ? anchor.position : fallback;

    private Vector3 ResolveSpot(Spot spot, RectTransform rt)
    {
        return spot switch
        {
            Spot.Left        => GetAnchorPos(leftAnchor,        rt.position),
            Spot.CenterLeft  => GetAnchorPos(centerLeftAnchor,  rt.position),
            Spot.Center      => GetAnchorPos(centerAnchor,      rt.position),
            Spot.CenterRight => GetAnchorPos(centerRightAnchor, rt.position),
            Spot.Right       => GetAnchorPos(rightAnchor,       rt.position),
            Spot.LeftOffscreen  => OffscreenFromAnchor(GetAnchorPos(leftAnchor,  rt.position), true),
            Spot.RightOffscreen => OffscreenFromAnchor(GetAnchorPos(rightAnchor, rt.position), false),
            _ => rt.position,
        };
    }

    private Vector3 OffscreenFromAnchor(Vector3 near, bool toLeft)
    {
        var p = near;
        p.x += toLeft ? -Screen.width * 0.6f : Screen.width * 0.6f;
        return p;
    }

    private Pose ComputeStartPose(PortraitModule module, RectTransform rt)
    {
        var cg = rt.GetComponent<CanvasGroup>();
        if (!cg) cg = rt.gameObject.AddComponent<CanvasGroup>();

        var startPos   = ResolveSpot(module.origin, rt);
        var startAlpha = module.useFade ? Mathf.Clamp01(module.enterFromOpacity / 100f) : cg.alpha;
        var startScale = rt.localScale;

        return new Pose(startPos, startAlpha, startScale);
    }

    private Pose ComputeEndPose(PortraitModule module, RectTransform rt)
    {
        var endPos  = ResolveSpot(module.target, rt);
        var toAlpha = module.useFade ? Mathf.Clamp01(module.enterToOpacity / 100f) : 1f;
        var endScale = rt.localScale;

        return new Pose(endPos, toAlpha, endScale);
    }

    #endregion

    #region IPortraitPlacementMilestones

    public IPortraitPlacementMilestones.Milestones ApplyWithMilestones(PortraitModule module)
    {
        var midTcs = new TaskCompletionSource<bool>();
        var endTcs = new TaskCompletionSource<bool>();

        _ = ApplyWithProgressAsync(module,
            progress => { if (progress >= 0.5f) midTcs.TrySetResult(true); },
            onComplete: () => endTcs.TrySetResult(true));

        return new IPortraitPlacementMilestones.Milestones(midTcs.Task, endTcs.Task);
    }

    private async Task ApplyWithProgressAsync(PortraitModule module, System.Action<float> onProgress, System.Action onComplete)
    {
        if (module == null || string.IsNullOrEmpty(module.profileId)
            || !_rootByProfile.TryGetValue(module.profileId, out var rootRt))
        {
            if (resetSpecialsWithStaticPoseOnNodeChange) ResetSpecialsToStaticPose();
            onProgress?.Invoke(1f);
            onComplete?.Invoke();
            return;
        }

        if (resetSpecialsWithStaticPoseOnNodeChange) ResetSpecialsToStaticPose();

        var img = _portraitByProfile[module.profileId];
        SetSpriteForModule(img, module);
        BringOnTop(module.profileId);
        UpdateTint(module.profileId);
        UpdateScale(module.profileId);
        _pendingSpecialByProfile.Remove(module.profileId);

        if (module.playSpecialAnimation && module.specialAnimation)
        {
            switch (module.specialStart)
            {
                case SpecialStartTiming.Immediate:
                case SpecialStartTiming.WithPlacementStart:
                    PlaySpecial(img, module.specialAnimation, module.specialAnimSpeed, module.specialAnimLoop);
                    break;
                default:
                    StopSpecial(img);
                    break;
            }
        }
        else
        {
            StopSpecial(img);
        }

        await RunPlacementAsync(module, rootRt, onProgress);

        if (module.playSpecialAnimation && module.specialAnimation &&
            (module.specialStart == SpecialStartTiming.WithPlacementComplete ||
             module.specialStart == SpecialStartTiming.WithTextStart))
        {
            PlaySpecial(img, module.specialAnimation, module.specialAnimSpeed, module.specialAnimLoop);
        }

        if (!(module.playSpecialAnimation && module.specialAnimation))
            StopSpecial(img);

        onComplete?.Invoke();
    }

    #endregion

    private void OnDisable()  => CleanupGraphs();
    private void OnDestroy()  => CleanupGraphs();

    private void CleanupGraphs()
    {
        foreach (var co in _placementCo.Values) if (co != null) StopCoroutine(co);
        _placementCo.Clear();
        foreach (var co in _scaleCo.Values) if (co != null) StopCoroutine(co);
        _scaleCo.Clear();
        foreach (var kv in _specialGraphs) if (kv.Value.IsValid()) kv.Value.Destroy();
        _specialGraphs.Clear();
    }
}
