using UnityEngine;

/// <summary>
/// Cámara 2D que sigue suavemente a un objetivo con opciones de:
/// - Suavizado (SmoothDamp)
/// - Velocidad máxima
/// - Look-ahead (anticipación en dirección del movimiento)
/// - Dead zone (no mover si el objetivo está cerca del centro)
/// - Límites opcionales por Collider2D (clamp dentro del mundo)
/// Colocar en la Main Camera (Orthographic).
/// </summary>
[RequireComponent(typeof(Camera))]
public class SmoothFollow2D : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;

    [Header("Ejes / Offset")]
    public bool followX = true;
    public bool followY = true;
    public Vector2 offset = Vector2.zero;

    [Header("Suavizado")]
    [Tooltip("Tiempo de suavizado (cuanto mayor, más 'lerdo').")]
    public float smoothTime = 0.15f;
    [Tooltip("Velocidad máxima a la que puede moverse la cámara.")]
    public float maxSpeed = 30f;
    [Tooltip("Si es true, al empezar la cámara se 'teletransporta' al objetivo (sin suavizado).")]
    public bool snapOnStart = true;

    [Header("Look Ahead (opcional)")]
    public bool useLookAhead = true;
    [Tooltip("Multiplicador de la velocidad del objetivo para anticipar.")]
    public float lookAheadFactor = 0.4f;
    [Tooltip("Distancia máxima de anticipación.")]
    public float lookAheadMax = 3f;
    [Tooltip("Qué rápido vuelve a 0 si el objetivo reduce velocidad.")]
    public float lookAheadReturnSpeed = 3f;

    [Header("Dead Zone (opcional)")]
    [Tooltip("Radio en unidades dentro del cual la cámara no se mueve.")]
    public float deadZoneRadius = 0f;

    [Header("Límites (opcional)")]
    [Tooltip("Si se asigna, la cámara no saldrá de este Collider2D (útil: BoxCollider2D del mapa).")]
    public Collider2D cameraBounds;

    // Internos
    private Camera _cam;
    private Vector3 _velocity = Vector3.zero;
    private Vector3 _lookAhead = Vector3.zero;
    private Vector3 _lastTargetPos;
    private bool _hasLast;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam.orthographic == false)
            Debug.LogWarning("[SmoothFollow2D] Esta pensado para cámara Orthographic.");
    }

    void Start()
    {
        if (target != null)
        {
            _lastTargetPos = target.position;
            _hasLast = true;

            if (snapOnStart)
            {
                Vector3 tp = transform.position;
                Vector3 desired = GetDesiredPosition(target.position, 0f);
                transform.position = new Vector3(desired.x, desired.y, tp.z);
                _velocity = Vector3.zero;
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        float dt = Mathf.Max(Time.deltaTime, 1e-6f);

        // 1) Calcular look-ahead según velocidad del objetivo
        if (useLookAhead && _hasLast)
        {
            Vector3 v = (target.position - _lastTargetPos) / dt; // velocidad aprox
            Vector2 v2 = new Vector2(v.x, v.y);

            // Anticipación proporcional a la velocidad
            Vector2 desiredLA = v2 * lookAheadFactor;
            if (desiredLA.magnitude > lookAheadMax)
                desiredLA = desiredLA.normalized * lookAheadMax;

            // Relajar hacia la anticipación deseada
            Vector2 la2 = Vector2.Lerp(new Vector2(_lookAhead.x, _lookAhead.y), desiredLA, dt * lookAheadReturnSpeed);
            _lookAhead = new Vector3(la2.x, la2.y, 0f);
        }
        else
        {
            // Si no usamos lookAhead, relajamos hacia 0
            _lookAhead = Vector3.Lerp(_lookAhead, Vector3.zero, dt * lookAheadReturnSpeed);
        }

        _lastTargetPos = target.position;
        _hasLast = true;

        // 2) Posición deseada (objetivo + offset + look-ahead)
        Vector3 desiredPos = GetDesiredPosition(target.position, dt);

        // 3) Dead zone: si el objetivo está cerca, no mover
        Vector3 current = transform.position;
        Vector2 delta2 = (Vector2)(desiredPos - current);
        if (deadZoneRadius > 0f && delta2.magnitude < deadZoneRadius)
        {
            // Mantener posición actual (no mover)
            desiredPos = new Vector3(current.x, current.y, desiredPos.z);
        }

        // 4) Suavizado
        Vector3 targetForSmooth = new Vector3(
            followX ? desiredPos.x : current.x,
            followY ? desiredPos.y : current.y,
            current.z // nunca movemos Z aquí
        );

        Vector3 newPos = Vector3.SmoothDamp(current, targetForSmooth, ref _velocity, smoothTime, maxSpeed);

        // 5) Clamp a límites del mundo (si hay)
        if (cameraBounds != null && _cam.orthographic)
            newPos = ClampToBounds(newPos, cameraBounds.bounds);

        transform.position = newPos;
    }

    private Vector3 GetDesiredPosition(Vector3 targetPos, float dt)
    {
        Vector3 desired = targetPos + new Vector3(offset.x, offset.y, 0f);
        desired += _lookAhead; // aplicar anticipación
        // Mantener Z actual de la cámara
        desired.z = transform.position.z;
        return desired;
    }

    private Vector3 ClampToBounds(Vector3 camPos, Bounds b)
    {
        if (!_cam) return camPos;

        float halfH = _cam.orthographicSize;
        float halfW = halfH * _cam.aspect;

        float minX = b.min.x + halfW;
        float maxX = b.max.x - halfW;
        float minY = b.min.y + halfH;
        float maxY = b.max.y - halfH;

        // Si el mapa es más pequeño que la vista, centra y no clamps
        if (minX > maxX) camPos.x = (b.min.x + b.max.x) * 0.5f;
        else camPos.x = Mathf.Clamp(camPos.x, minX, maxX);

        if (minY > maxY) camPos.y = (b.min.y + b.max.y) * 0.5f;
        else camPos.y = Mathf.Clamp(camPos.y, minY, maxY);

        return camPos;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Dead zone visual
        if (deadZoneRadius > 0f)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, deadZoneRadius);
        }

        // Bounds visual
        if (cameraBounds != null)
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
            Gizmos.DrawWireCube(cameraBounds.bounds.center, cameraBounds.bounds.size);
        }
    }
#endif
}
