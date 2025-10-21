using UnityEngine;

[AddComponentMenu("Kimera/Teleport Portal 2D")]
[RequireComponent(typeof(CircleCollider2D))]
[ExecuteAlways]
public class TeleportPortal2D : MonoBehaviour
{
    [Header("Uso")]
    [Tooltip("Tecla para activar el teletransporte estando dentro del radio.")]
    public KeyCode useKey = KeyCode.E;

    [Header("Destino (vector independiente por portal)")]
    [Tooltip("Coordenadas de mundo donde caerá el jugador.")]
    public Vector2 teleportPoint;

    [Header("Radio del portal")]
    [Tooltip("Radio de activación (se sincroniza con el CircleCollider2D).")]
    public float areaRadius = 2f;

    [Tooltip("Sincroniza automáticamente con el CircleCollider2D.radius.")]
    public bool syncColliderRadius = true;

    [Header("Seguridad / Colisiones")]
    [Tooltip("Radio libre mínimo en el destino. Si está ocupado, no hace TP.")]
    public float checkRadius = 0.25f;
    [Tooltip("Capas que bloquean el punto de llegada.")]
    public LayerMask obstacleLayers;

    [Header("Filtro de quién puede usarlo")]
    public bool requireTag = true;
    public string requiredTag = "Player";

    [Tooltip("Poner la velocidad del Rigidbody2D a cero al teletransportar.")]
    public bool resetVelocityOnTeleport = true;

    [Header("Gizmos (solo visual)")]
    public bool drawGizmos = true;
    public Color areaFillColor = new Color(0.2f, 0.6f, 1f, 0.10f);
    public Color areaOutlineColor = new Color(0.2f, 0.6f, 1f, 0.9f);
    public Color targetFillColor = new Color(0.2f, 1f, 0.4f, 0.25f);
    public Color targetOutlineColor = new Color(0.2f, 1f, 0.4f, 1f);
    public Color linkLineColor = new Color(0.2f, 0.8f, 1f, 0.8f);
    public float targetCrossSize = 0.18f;

    CircleCollider2D _col;

    bool _inside;
    Rigidbody2D _insideRb;

    void Reset()
    {
        _col = GetComponent<CircleCollider2D>();
        _col.isTrigger = true;
        _col.radius = areaRadius;
        if (teleportPoint == Vector2.zero)
            teleportPoint = (Vector2)transform.position; // + Vector2.up // por defecto un poco arriba eliminado
    }

    void Awake()
    {
        _col = GetComponent<CircleCollider2D>();
        if (_col) _col.isTrigger = true;
    }

    void OnEnable() => SyncCollider();
    void OnValidate()
    {
        if (areaRadius < 0f) areaRadius = 0f;
        if (checkRadius < 0f) checkRadius = 0f;
        SyncCollider();
    }
    void Update()
    {
        if (!Application.isPlaying)
        {
            SyncCollider();
            return;
        }

        if (_inside && _insideRb != null && Input.GetKeyDown(useKey))
        {
            if (Physics2D.OverlapCircle(teleportPoint, checkRadius, obstacleLayers) == null)
            {
                _insideRb.position = teleportPoint;
                if (resetVelocityOnTeleport) _insideRb.linearVelocity = Vector2.zero; // Unity 6
            }
        }
    }

    void SyncCollider()
    {
        if (!_col) _col = GetComponent<CircleCollider2D>();
        if (_col && syncColliderRadius)
        {
            _col.isTrigger = true;
            if (_col.radius != areaRadius) _col.radius = areaRadius;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;
        if (requireTag && !other.transform.CompareTag(requiredTag)) return;

        _inside = true;
        _insideRb = rb;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.attachedRigidbody == _insideRb)
        {
            _inside = false;
            _insideRb = null;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Área
        UnityEditor.Handles.color = areaFillColor;
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.forward, areaRadius);
        UnityEditor.Handles.color = areaOutlineColor;
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, areaRadius);

        // Línea al destino
        Gizmos.color = linkLineColor;
        Gizmos.DrawLine(transform.position, new Vector3(teleportPoint.x, teleportPoint.y, transform.position.z));

        // Destino
        UnityEditor.Handles.color = targetFillColor;
        UnityEditor.Handles.DrawSolidDisc(teleportPoint, Vector3.forward, checkRadius);
        UnityEditor.Handles.color = targetOutlineColor;
        UnityEditor.Handles.DrawWireDisc(teleportPoint, Vector3.forward, checkRadius);

        Gizmos.color = targetOutlineColor;
        Vector3 tp3 = new Vector3(teleportPoint.x, teleportPoint.y, 0f);
        Gizmos.DrawLine(tp3 + Vector3.left * targetCrossSize, tp3 + Vector3.right * targetCrossSize);
        Gizmos.DrawLine(tp3 + Vector3.down * targetCrossSize, tp3 + Vector3.up * targetCrossSize);
    }
#endif

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(TeleportPortal2D)), UnityEditor.CanEditMultipleObjects]
    class TeleportPortal2DEditor : UnityEditor.Editor
    {
        void OnSceneGUI()
        {
            var tp = (TeleportPortal2D)target;

            // Handle del radio (del propio portal seleccionado)
            UnityEditor.EditorGUI.BeginChangeCheck();
            float r = UnityEditor.Handles.RadiusHandle(Quaternion.identity, tp.transform.position, tp.areaRadius);
            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                UnityEditor.Undo.RecordObject(tp, "Change Portal Radius");
                tp.areaRadius = Mathf.Max(0f, r);
                if (tp.syncColliderRadius && tp.TryGetComponent(out CircleCollider2D col))
                {
                    UnityEditor.Undo.RecordObject(col, "Sync Collider Radius");
                    col.radius = tp.areaRadius;
                    col.isTrigger = true;
                    UnityEditor.EditorUtility.SetDirty(col);
                }
                UnityEditor.EditorUtility.SetDirty(tp);
            }

            // Handle 2D para mover el vector de destino (XY)
            Vector3 p = new Vector3(tp.teleportPoint.x, tp.teleportPoint.y, 0f);
            float size = UnityEditor.HandleUtility.GetHandleSize(p) * 0.12f;
            UnityEditor.Handles.color = new Color(0.2f, 1f, 0.4f, 1f);

            UnityEditor.EditorGUI.BeginChangeCheck();
            Vector3 newP = UnityEditor.Handles.Slider2D(
                p,
                Vector3.forward,       // plano XY
                Vector3.right,
                Vector3.up,
                size,
                UnityEditor.Handles.SphereHandleCap,
                0f                     // sin snap (usa tecla V para vertex snap si quieres)
            );
            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                UnityEditor.Undo.RecordObject(tp, "Move Teleport Point");
                tp.teleportPoint = new Vector2(newP.x, newP.y);
                UnityEditor.EditorUtility.SetDirty(tp);
            }
        }
    }
#endif
}
