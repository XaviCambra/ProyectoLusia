using UnityEngine;

/// <summary>
/// Modulo complementario de Interactable: al accionar, teletransporta al personaje a un punto
/// fijo (arrastrable en Scene view o editable en el Inspector), igual que hace
/// TeleportPortal2D pero como pieza reusable sobre el componente base de interaccion.
/// </summary>
[RequireComponent(typeof(Interactable))]
public class InteractableTeleportModule : MonoBehaviour
{
    [Header("Destino")]
    [Tooltip("Coordenadas de mundo donde caera el personaje.")]
    public Vector2 teleportPoint;

    [Header("Seguridad / Colisiones")]
    [Tooltip("Radio libre minimo en el destino. Si esta ocupado, no hace TP.")]
    public float checkRadius = 0.25f;
    [Tooltip("Capas que bloquean el punto de llegada. Vacio (Nothing) = sin comprobacion.")]
    public LayerMask obstacleLayers;

    [Tooltip("Poner la velocidad del Rigidbody2D a cero al teletransportar.")]
    public bool resetVelocityOnTeleport = true;

    [Header("Gizmos (solo visual)")]
    public bool drawGizmos = true;
    public Color targetFillColor = new Color(1f, 1f, 1f, 0.25f);
    public Color targetOutlineColor = new Color(1f, 1f, 1f, 1f);
    public Color linkLineColor = new Color(1f, 1f, 1f, 0.8f);
    public float targetCrossSize = 0.18f;

    Interactable _interactable;

    void Awake() => _interactable = GetComponent<Interactable>();

    void OnEnable()
    {
        if (!_interactable) _interactable = GetComponent<Interactable>();
        _interactable.OnInteract += HandleInteract;
    }

    void OnDisable()
    {
        if (_interactable) _interactable.OnInteract -= HandleInteract;
    }

    void HandleInteract(GameObject character)
    {
        if (Physics2D.OverlapCircle(teleportPoint, checkRadius, obstacleLayers) != null) return;

        var rb = character.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        rb.position = teleportPoint;
        if (resetVelocityOnTeleport) rb.linearVelocity = Vector2.zero;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = linkLineColor;
        Gizmos.DrawLine(transform.position, new Vector3(teleportPoint.x, teleportPoint.y, transform.position.z));

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
    [UnityEditor.CustomEditor(typeof(InteractableTeleportModule)), UnityEditor.CanEditMultipleObjects]
    class InteractableTeleportModuleEditor : UnityEditor.Editor
    {
        void OnSceneGUI()
        {
            var mod = (InteractableTeleportModule)target;

            Vector3 p = new Vector3(mod.teleportPoint.x, mod.teleportPoint.y, 0f);
            float size = UnityEditor.HandleUtility.GetHandleSize(p) * 0.12f;
            UnityEditor.Handles.color = mod.targetOutlineColor;

            UnityEditor.EditorGUI.BeginChangeCheck();
            Vector3 newP = UnityEditor.Handles.Slider2D(
                p,
                Vector3.forward,
                Vector3.right,
                Vector3.up,
                size,
                UnityEditor.Handles.SphereHandleCap,
                0f
            );
            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                UnityEditor.Undo.RecordObject(mod, "Move Teleport Point");
                mod.teleportPoint = new Vector2(newP.x, newP.y);
                UnityEditor.EditorUtility.SetDirty(mod);
            }
        }
    }
#endif
}
