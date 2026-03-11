using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

// =============================================================
// PortraitController — implementa IPortraitController
// Gestiona: pool de retratos por perfil, orden visual, tinte/escala, y
// animaciones de colocación (Cut/Fade/Slide) + animaciones especiales (Playables).
// =============================================================

[DisallowMultipleComponent]
public sealed class PortraitController : MonoBehaviour, IPortraitController
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

    #endregion

    #region Estado interno

    private DialogueGraph _graph;

    private readonly Dictionary<CharacterProfile, Image>         _portraitByProfile = new();
    private readonly Dictionary<CharacterProfile, RectTransform> _rootByProfile     = new();
    private readonly Dictionary<Image, SpecialAnimState> _specialGraphs    = new();
    private readonly HashSet<Image>                      _persistentEmotes  = new();
    private readonly Dictionary<RectTransform, Coroutine> _placementCo = new();
    private readonly Dictionary<RectTransform, Coroutine> _scaleCo     = new();
    private readonly Dictionary<CharacterProfile, TaskCompletionSource<bool>> _placementTcs = new();

    private struct Pose
    {
        public Vector3 pos;
        public float   alpha;
        public Pose(Vector3 p, float a) { pos = p; alpha = a; }
    }

    private readonly struct SpecialAnimState
    {
        public readonly PlayableGraph          Graph;
        public readonly AnimationClipPlayable  Playable;
        public readonly AnimationClip          Clip;
        public readonly float                  Speed;
        public SpecialAnimState(PlayableGraph g, AnimationClipPlayable p, AnimationClip c, float s)
        { Graph = g; Playable = p; Clip = c; Speed = s; }
    }

    #endregion

    #region IPortraitController

    public void Init(DialogueGraph graph)
    {
        _graph = graph;
        RebuildPool();
    }

    public async Task ApplyAsync(PortraitModule module)
    {
        if (module == null || module.profileRef == null
            || !_rootByProfile.TryGetValue(module.profileRef, out var rootRt))
        {
            if (resetSpecialsWithStaticPoseOnNodeChange) ResetSpecialsToStaticPose();
            return;
        }

        if (resetSpecialsWithStaticPoseOnNodeChange) ResetSpecialsToStaticPose();

        var img = _portraitByProfile[module.profileRef];
        SetSpriteForModule(img, module);

        BringOnTop(module.profileRef);
        UpdateTint(module.profileRef);
        UpdateScale(module.profileRef);

        await RunPlacementAsync(module, rootRt);
    }

    public void PlaySpecialAnimation(CharacterProfile profile, AnimationClip clip, float speed, bool loop, bool persistent = false)
    {
        if (profile == null || !_portraitByProfile.TryGetValue(profile, out var img) || !img) return;
        if (persistent) _persistentEmotes.Add(img);
        else            _persistentEmotes.Remove(img);
        PlaySpecial(img, clip, speed, loop);
    }

    public void StopSpecialAnimation(CharacterProfile profile)
    {
        if (profile == null || !_portraitByProfile.TryGetValue(profile, out var img) || !img) return;
        _persistentEmotes.Remove(img);
        StopSpecial(img, graceful: true);
    }

    public void SnapPlacement(PortraitModule module)
    {
        if (module == null || module.profileRef == null) return;
        var profile = module.profileRef;
        if (!_rootByProfile.TryGetValue(profile, out var rootRt) || !rootRt) return;

        // Detener coroutine activa si existe
        if (_placementCo.TryGetValue(rootRt, out var co) && co != null)
        {
            StopCoroutine(co);
            _placementCo.Remove(rootRt);
        }

        // Calcular end pose directamente del módulo (no depende del caché)
        if (module.appearance != AppearanceMode.None)
        {
            var cg = rootRt.GetComponent<CanvasGroup>();
            if (!cg) cg = rootRt.gameObject.AddComponent<CanvasGroup>();

            rootRt.position = ResolveSpot(module.target, rootRt);
            cg.alpha        = module.useFade ? UnityEngine.Mathf.Clamp01(module.enterToOpacity / 100f) : 1f;
        }

        if (_placementTcs.TryGetValue(profile, out var tcs))
        {
            _placementTcs.Remove(profile);
            tcs.TrySetResult(true);
        }
    }

    public void ResetAll()
    {
        foreach (var kv in _placementCo) if (kv.Value != null) StopCoroutine(kv.Value);
        _placementCo.Clear();
        foreach (var t in _placementTcs.Values) t.TrySetResult(true);
        _placementTcs.Clear();
        foreach (var kv in _scaleCo) if (kv.Value != null) StopCoroutine(kv.Value);
        _scaleCo.Clear();
        foreach (var kv in _specialGraphs) if (kv.Value.Graph.IsValid()) kv.Value.Graph.Destroy();
        _specialGraphs.Clear();
        _persistentEmotes.Clear();
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
        foreach (var t in _placementTcs.Values) t.TrySetResult(true);
        _placementTcs.Clear();
        foreach (var kv in _scaleCo) if (kv.Value != null) StopCoroutine(kv.Value);
        _scaleCo.Clear();
        foreach (var kv in _specialGraphs) if (kv.Value.Graph.IsValid()) kv.Value.Graph.Destroy();
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
            .Where(m => m.profileRef != null)
            .Select(m => m.profileRef)
            .Distinct();

        foreach (var profile in uniqueProfiles)
        {
            var rootGo = new GameObject($"PortraitRoot_{profile.name}", typeof(RectTransform), typeof(CanvasGroup));
            var rootRt = (RectTransform)rootGo.transform;
            rootRt.SetParent(portraitsRoot, false);

            if (centerAnchor) rootRt.position = centerAnchor.position;
            else { rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0.5f); rootRt.anchoredPosition = Vector2.zero; }
            rootRt.localScale = Vector3.one;

            var cg = rootGo.GetComponent<CanvasGroup>();
            cg.alpha = 0f;

            var img = Instantiate(portraitPrefab, rootRt);
            img.gameObject.name = $"Portrait_{profile.name}";
            img.enabled = false;

            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            _portraitByProfile[profile] = img;
            _rootByProfile[profile]     = rootRt;
        }
    }

    private void SetSpriteForModule(Image img, PortraitModule module)
    {
        if (!img || module == null || module.profileRef == null) return;

        var sprite  = module.profileRef.GetPortraitByKey(module.portraitKey);
        img.sprite  = sprite;
        img.enabled = sprite != null;
    }

    #endregion

    #region Orden, tinte y escala

    private void BringOnTop(CharacterProfile profile)
    {
        if (profile == null) return;
        if (_portraitByProfile.TryGetValue(profile, out var img) && img)
            img.transform.SetAsLastSibling();
    }

    private void UpdateTint(CharacterProfile current)
    {
        if (_portraitByProfile.Count == 0) return;
        foreach (var kv in _portraitByProfile)
        {
            var img = kv.Value; if (!img) continue;
            if (!dimNonSpeaking || current == null) { img.color = speakingTint; continue; }
            img.color = (kv.Key == current) ? speakingTint : nonSpeakingTint;
        }
    }

    private void UpdateScale(CharacterProfile current)
    {
        if (_portraitByProfile.Count == 0) return;
        foreach (var kv in _portraitByProfile)
        {
            var img = kv.Value; if (!img) continue;
            var rt  = img.rectTransform;
            var target = (!scaleNonSpeaking || current == null)
                ? speakingScale
                : (kv.Key == current ? speakingScale : nonSpeakingScale);
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

    private async Task RunPlacementAsync(PortraitModule module, RectTransform rootRt)
    {
        if (!rootRt) return;

        // None = solo actualiza sprite/tinte/escala; no toca posición ni alpha.
        if (module.appearance == AppearanceMode.None) return;

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
                return;

            case AppearanceMode.Slide:
                if (module.moveSpeed <= 1f)
                {
                    rootRt.position = to.pos;
                    cg.alpha        = to.alpha;
                    return;
                }
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _placementTcs[module.profileRef] = tcs;
                _placementCo[rootRt] = StartCoroutine(CoPlaceAndComplete(rootRt, cg, from, to, module.moveSpeed, module.profileRef, tcs));
                await tcs.Task;
                return;
        }
    }

    private IEnumerator CoPlace(RectTransform rt, CanvasGroup cg, Pose from, Pose to, float speed)
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

            yield return null;
        }

        rt.position = to.pos;
        if (cg) cg.alpha = to.alpha;
    }

    private IEnumerator CoPlaceAndComplete(RectTransform rt, CanvasGroup cg, Pose from, Pose to, float speed,
        CharacterProfile profile, TaskCompletionSource<bool> tcs)
    {
        yield return CoPlace(rt, cg, from, to, speed);
        _placementCo.Remove(rt);
        _placementTcs.Remove(profile);
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

        var graph        = PlayableGraph.Create($"DG_SpecialAnim_{img.name}");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        var output       = AnimationPlayableOutput.Create(graph, "AnimOutput", animator);
        var clipPlayable = AnimationClipPlayable.Create(graph, clip);
        clipPlayable.SetSpeed(Mathf.Approximately(speed, 0f) ? 0f : speed);
        clipPlayable.SetApplyFootIK(false);

        output.SetSourcePlayable(clipPlayable);
        graph.Play();
        _specialGraphs[img] = new SpecialAnimState(graph, clipPlayable, clip, speed);

        if (!loop && clip.length > 0f && speed > 0f)
            StartCoroutine(FinishAndReset(graph, img, clip.length / speed));
    }

    // Espera a que acabe el tiempo indicado (fin de clip o fin de loop actual), destruye el grafo y resetea la pose.
    private IEnumerator FinishAndReset(PlayableGraph g, Image img, float delay)
    {
        float t = 0f;
        while (t < delay && g.IsValid()) { t += Time.deltaTime; yield return null; }
        if (g.IsValid()) g.Destroy();
        _specialGraphs.Remove(img);
        ResetPortraitPose(img);
    }

    // graceful=true: completa el loop actual antes de parar. graceful=false: corte inmediato.
    private void StopSpecial(Image img, bool graceful = false)
    {
        if (!img) return;
        if (!_specialGraphs.TryGetValue(img, out var state)) return;
        _specialGraphs.Remove(img);
        if (!state.Graph.IsValid()) return;

        if (graceful && state.Clip && state.Speed > 0f)
        {
            float elapsed   = (float)state.Playable.GetTime();
            float loopLen   = state.Clip.length / state.Speed;
            float remaining = loopLen - (elapsed % loopLen);
            StartCoroutine(FinishAndReset(state.Graph, img, remaining));
        }
        else
        {
            state.Graph.Destroy();
            ResetPortraitPose(img);
        }
    }

    private void ResetPortraitPose(Image img)
    {
        if (!img) return;
        if (specialStaticPoseClip)
            specialStaticPoseClip.SampleAnimation(img.gameObject, 0f);
        else
        {
            img.rectTransform.localPosition = Vector3.zero;
            img.rectTransform.localRotation = Quaternion.identity;
            img.rectTransform.localScale    = Vector3.one;
        }
    }

    private void ResetSpecialsToStaticPose()
    {
        if (!resetSpecialsWithStaticPoseOnNodeChange) return;
        if (_specialGraphs.Count == 0) return;

        foreach (var img in _specialGraphs.Keys.ToList())
        {
            if (_persistentEmotes.Contains(img)) continue;
            StopSpecial(img); // inmediato + reset de pose
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
        return new Pose(ResolveSpot(module.origin, rt),
                        module.useFade ? Mathf.Clamp01(module.enterFromOpacity / 100f) : cg.alpha);
    }

    private Pose ComputeEndPose(PortraitModule module, RectTransform rt)
        => new Pose(ResolveSpot(module.target, rt),
                    module.useFade ? Mathf.Clamp01(module.enterToOpacity / 100f) : 1f);

    #endregion

    private void OnDisable()  => CleanupGraphs();
    private void OnDestroy()  => CleanupGraphs();

    private void CleanupGraphs()
    {
        foreach (var co in _placementCo.Values) if (co != null) StopCoroutine(co);
        _placementCo.Clear();
        foreach (var t in _placementTcs.Values) t.TrySetResult(true);
        _placementTcs.Clear();
        foreach (var co in _scaleCo.Values) if (co != null) StopCoroutine(co);
        _scaleCo.Clear();
        foreach (var kv in _specialGraphs) if (kv.Value.Graph.IsValid()) kv.Value.Graph.Destroy();
        _specialGraphs.Clear();
        _persistentEmotes.Clear();
    }
}
