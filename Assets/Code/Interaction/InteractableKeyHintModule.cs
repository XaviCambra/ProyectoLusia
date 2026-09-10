using UnityEngine;

/// <summary>
/// Modulo complementario de Interactable: mientras el jugador esta dentro de un radio propio
/// (hintRadius, normalmente un poco mayor que el Activation Radius del Interactable), instancia
/// un prefab de aviso y ajusta su transparencia y posicion segun la distancia real al jugador,
/// usando una curva para controlar la transicion en vez de aparecer/desaparecer de golpe.
/// El prefab necesita un CanvasGroup en su raiz -- si no lo tiene, este modulo se lo anade solo,
/// asi no hace falta ir componente por componente (Image, texto...) ajustando el alpha a mano.
/// </summary>
[RequireComponent(typeof(Interactable))]
public class InteractableKeyHintModule : MonoBehaviour
{
    [Header("Prefab de aviso")]
    [Tooltip("Prefab que se muestra segun la distancia al jugador. Necesita un CanvasGroup en la raiz (si no lo tiene, se le anade automaticamente).")]
    public GameObject promptPrefab;
    [Tooltip("Punto base ('punto 0') del que parte el aviso. Si esta vacio, usa este mismo transform.")]
    public Transform anchor;

    [Header("Deteccion")]
    [Tooltip("Tag del jugador a seguir (independiente del filtro de contacto del Interactable).")]
    public string playerTag = "Player";
    [Tooltip("Radio en el que el aviso empieza a mostrarse. Normalmente algo mayor que el Activation Radius del Interactable, para que se anticipe antes de poder interactuar.")]
    public float hintRadius = 2.5f;

    [Header("Curva de aproximacion")]
    [Tooltip("Eje X: 0 = borde de Hint Radius (recien entra), 1 = dentro del Activation Radius del Interactable. Eje Y: fuerza del efecto (alpha y movimiento).")]
    public AnimationCurve proximityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Movimiento hacia el jugador")]
    public bool moveTowardsPlayer = true;
    [Range(0f, 1f)]
    [Tooltip("Cuanto se acerca al jugador como maximo (0 = no se mueve, se queda en el punto 0; 1 = llega hasta el jugador).")]
    public float followStrength = 0.4f;

    [Header("Gizmos (solo visual)")]
    public bool drawGizmos = true;
    public Color hintRadiusColor = new Color(1f, 1f, 1f, 0.6f);

    Interactable _interactable;
    GameObject _promptInstance;
    CanvasGroup _canvasGroup;
    GameObject _player;

    void Awake() => _interactable = GetComponent<Interactable>();

    void Start()
    {
        if (!_interactable) _interactable = GetComponent<Interactable>();
        EnsurePromptInstance();
        _player = GameObject.FindGameObjectWithTag(playerTag);
    }

    void Update()
    {
        if (!_promptInstance) return;

        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag(playerTag);
            if (_player == null)
            {
                _promptInstance.SetActive(false);
                return;
            }
        }

        Vector3 basePos = anchor ? anchor.position : transform.position;
        float distance = Vector2.Distance(basePos, _player.transform.position);
        float t = Mathf.InverseLerp(hintRadius, _interactable.activationRadius, distance);

        if (t <= 0f)
        {
            if (_promptInstance.activeSelf) _promptInstance.SetActive(false);
            return;
        }

        if (!_promptInstance.activeSelf) _promptInstance.SetActive(true);

        float strength = proximityCurve.Evaluate(t);

        if (_canvasGroup) _canvasGroup.alpha = strength;

        _promptInstance.transform.position = moveTowardsPlayer
            ? Vector3.Lerp(basePos, _player.transform.position, strength * followStrength)
            : basePos;
    }

    GameObject EnsurePromptInstance()
    {
        if (_promptInstance) return _promptInstance;
        if (!promptPrefab)
        {
            Debug.LogWarning($"{nameof(InteractableKeyHintModule)} en '{name}' no tiene promptPrefab asignado.", this);
            return null;
        }

        var parent = anchor ? anchor : transform;
        _promptInstance = Instantiate(promptPrefab, parent);
        _promptInstance.transform.position = parent.position;

        _canvasGroup = _promptInstance.GetComponent<CanvasGroup>();
        if (!_canvasGroup) _canvasGroup = _promptInstance.AddComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        _promptInstance.SetActive(false);
        return _promptInstance;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Vector3 basePos = anchor ? anchor.position : transform.position;
        UnityEditor.Handles.color = hintRadiusColor;
        UnityEditor.Handles.DrawWireDisc(basePos, Vector3.forward, hintRadius);
    }
#endif
}
