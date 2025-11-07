using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

// =============================================================
// Proyecto Kimera — PortraitController (implementa IPortraitController)
// Gestiona: pool de retratos por perfil, orden visual, tinte/escala, y
// animaciones de colocación (Cut/Fade/Slide) + animaciones especiales (Playables).
// =============================================================


[DisallowMultipleComponent]
public sealed class PortraitController : MonoBehaviour, IPortraitController
{
    #region Inspector

    [Header("Anchors de posición")]
    [SerializeField] private RectTransform leftAnchor;
    [SerializeField] private RectTransform centerAnchor;
    [SerializeField] private RectTransform rightAnchor;

    [Header("Pool de retratos")]
    [SerializeField] private RectTransform portraitsRoot;
    [SerializeField] private Image portraitPrefab;

    [Header("Tinte / escalado")]
    [SerializeField] private bool dimNonSpeaking = true;
    [SerializeField] private Color speakingTint = Color.white;
    [SerializeField] private Color nonSpeakingTint = new(1f, 1f, 1f, 0.50f);
    [SerializeField] private bool scaleNonSpeaking = true;
    [SerializeField] private Vector3 speakingScale = Vector3.one;
    [SerializeField] private Vector3 nonSpeakingScale = new(0.95f, 0.95f, 0.95f);
    [SerializeField, Min(0f)] private float scaleTweenDuration = 0.15f;
    [SerializeField] private AnimationCurve scaleTweenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Movimiento de colocación")]
    [SerializeField] private AnimationCurve moveCurve = null; // si es null → lineal

    [Header("Animación especial")]
    [SerializeField] private bool resetSpecialsWithStaticPoseOnNodeChange = true;
    [SerializeField] private AnimationClip specialStaticPoseClip;

    #endregion

    #region Estado interno

    private DialogueGraph _graph;

    private readonly Dictionary<string, Image> _portraitByProfile = new();
    private readonly Dictionary<string, RectTransform> _rootByProfile = new();
    private readonly Dictionary<Image, PlayableGraph> _specialGraphs = new();

    // Coroutines en curso por raíz (entrada/salida)
    private readonly Dictionary<RectTransform, Coroutine> _placementCo = new();
    private readonly Dictionary<RectTransform, Coroutine> _scaleCo = new();

    private readonly struct PortraitState
    {
        public readonly Vector3 screenPos;
        public PortraitState(Vector3 p) { screenPos = p; }
    }
    private readonly Dictionary<string, PortraitState> _stateByProfile = new();

    private ICharacterProfileService _profiles;

    #endregion

    #region IPortraitController

    public void Init(DialogueGraph graph)
    {
        _graph = graph;
        _profiles = FindAnyObjectByType<CharacterProfileService>();
        RebuildPool();
    }

    public async Task ApplyAsync(DialogueNodeData node)
    {
        if (node == null || string.IsNullOrEmpty(node.profileId) || !_rootByProfile.TryGetValue(node.profileId, out var rootRt))
        {
            // Aun así, si hubiera animaciones especiales previas, se pueden resetear
            if (resetSpecialsWithStaticPoseOnNodeChange)
                ResetSpecialsToStaticPose();
            return;
        }

        // Reset specials opcional al cambiar de nodo
        if (resetSpecialsWithStaticPoseOnNodeChange)
            ResetSpecialsToStaticPose();

        // 1) Asignar sprite al retrato del perfil
        var img = _portraitByProfile[node.profileId];
        SetSpriteForNode(img, node);

        // 2) Orden visual + tinte/escala por locutor
        BringOnTop(node.profileId);
        UpdateTint(node.profileId);
        UpdateScale(node.profileId);

        // 3) Colocación (Cut/Fade/Slide) — devolver cuando termine la anim. principal
        await RunPlacementAsync(node, rootRt);

        // 4) Animación especial (si procede)
        if (node.playSpecialAnimation && node.specialAnimation)
            PlaySpecial(img, node.specialAnimation, node.specialAnimSpeed, node.specialAnimLoop);
        else
            StopSpecial(img); // asegura que no queden loops antiguos
    }

    public void ResetAll()
    {
        // Parar coroutines
        foreach (var kv in _placementCo) if (kv.Value != null) StopCoroutine(kv.Value);
        _placementCo.Clear();
        foreach (var kv in _scaleCo) if (kv.Value != null) StopCoroutine(kv.Value);
        _scaleCo.Clear();

        // Destruir graphs
        foreach (var kv in _specialGraphs) if (kv.Value.IsValid()) kv.Value.Destroy();
        _specialGraphs.Clear();

        // Ocultar retratos
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
        // Limpiar lo existente
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

        if (_graph == null || portraitsRoot == null || portraitPrefab == null)
            return;

        var uniqueProfiles = _graph.Nodes
            .Where(n => n != null && !string.IsNullOrEmpty(n.profileId))
            .Select(n => n.profileId)
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
            _rootByProfile[pid] = rootRt;
            _stateByProfile[pid] = new PortraitState(rootRt.position);
        }
    }

    private void SetSpriteForNode(Image img, DialogueNodeData node)
    {
        if (!img || node == null || _profiles == null) return;

        Sprite sprite = null;
        if (!string.IsNullOrEmpty(node.profileId))
        {
            var p = _profiles.GetById(node.profileId);
            if (p != null)
            {
                if (!string.IsNullOrEmpty(node.portraitKey))
                    sprite = p.GetPortraitByKey(node.portraitKey);
                if (!sprite) sprite = p.GetPortraitByKey("Default");
                if (!sprite)
                {
                    var firstKey = p.GetPortraitKeys().FirstOrDefault();
                    if (!string.IsNullOrEmpty(firstKey)) sprite = p.GetPortraitByKey(firstKey);
                }
            }
        }

        img.sprite = sprite;
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
            var rt = img.rectTransform;

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

    private async Task RunPlacementAsync(DialogueNodeData node, RectTransform rootRt)
    {
        if (!rootRt)
            return;

        // Cancelar animación previa en esta raíz
        if (_placementCo.TryGetValue(rootRt, out var running) && running != null)
        {
            StopCoroutine(running);
            _placementCo.Remove(rootRt);
        }

        var cg = rootRt.GetComponent<CanvasGroup>();
        if (!cg) cg = rootRt.gameObject.AddComponent<CanvasGroup>();

        Vector3 startPos = ResolveSpot(node.origin, rootRt);
        rootRt.position = startPos;

        bool exiting = (node.target == Spot.LeftOffscreen || node.target == Spot.RightOffscreen);
        Vector3 endPos = ResolveSpot(node.target, rootRt);

        float fromA = 1f, toA = 1f;
        if (node.useFade)
        {
            if (exiting) { fromA = node.exitFromOpacity / 100f; toA = node.exitToOpacity / 100f; }
            else { fromA = node.enterFromOpacity / 100f; toA = node.enterToOpacity / 100f; }
        }
        cg.alpha = fromA;

        switch (node.appearance)
        {
            case AppearanceMode.Cut:
                rootRt.position = endPos;
                cg.alpha = toA;
                _stateByProfile[node.profileId] = new PortraitState(rootRt.position);
                return;

            case AppearanceMode.Fade:
                {
                    await RunAsTask(CoFade(rootRt, cg, toA));
                    _stateByProfile[node.profileId] = new PortraitState(rootRt.position);
                    return;
                }
            case AppearanceMode.Slide:
                {
                    if (node.moveSpeed <= 1f)
                    {
                        rootRt.position = endPos;
                        cg.alpha = toA;
                        _stateByProfile[node.profileId] = new PortraitState(rootRt.position);
                        return;
                    }
                    await RunAsTask(CoSlideAndFade(rootRt, cg, endPos, toA, node.moveSpeed));
                    _stateByProfile[node.profileId] = new PortraitState(rootRt.position);
                    return;
                }
            default:
                return;
        }
    }

    private IEnumerator CoFade(RectTransform rootRt, CanvasGroup cg, float endAlpha)
    {
        float start = cg.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, endAlpha, t);
            yield return null;
        }
    }

    private IEnumerator CoSlideAndFade(RectTransform rt, CanvasGroup cg, Vector3 endPos, float endAlpha, float speed)
    {
        Vector3 startPos = rt.position;
        float startAlpha = cg.alpha;

        float dist = Vector3.Distance(startPos, endPos);
        if (dist <= Mathf.Epsilon)
        {
            // Solo fade
            float t0 = 0f;
            while (t0 < 1f)
            {
                t0 += Time.deltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t0);
                yield return null;
            }
            yield break;
        }

        float duration = dist / Mathf.Max(0.0001f, speed);
        float elapsed = 0f;
        var curve = (moveCurve != null) ? moveCurve : AnimationCurve.Linear(0f, 0f, 1f, 1f);

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

        var output = AnimationPlayableOutput.Create(graph, "AnimOutput", animator);
        var clipPlayable = AnimationClipPlayable.Create(graph, clip);
        clipPlayable.SetSpeed(Mathf.Approximately(speed, 0f) ? 0f : speed);
        clipPlayable.SetApplyFootIK(false);
        // clipPlayable.SetRemoveStartOffset(true); // No disponible en algunas versiones de Unity, se omite

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
        if (!resetSpecialsWithStaticPoseOnNodeChange) return;
        if (!specialStaticPoseClip) return;
        if (_specialGraphs.Count == 0) return;

        // snapshot para evitar modificar el dic mientras iteramos
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
            Spot.Left => GetAnchorPos(leftAnchor, rt.position),
            Spot.CenterLeft => GetAnchorPos(leftAnchor ? leftAnchor : centerAnchor, rt.position),
            Spot.Center => GetAnchorPos(centerAnchor, rt.position),
            Spot.CenterRight => GetAnchorPos(rightAnchor ? rightAnchor : centerAnchor, rt.position),
            Spot.Right => GetAnchorPos(rightAnchor, rt.position),
            Spot.LeftOffscreen => OffscreenFromAnchor(GetAnchorPos(leftAnchor, rt.position), true),
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

    #endregion
}

