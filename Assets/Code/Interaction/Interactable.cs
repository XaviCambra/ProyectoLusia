using System;
using UnityEngine;

/// <summary>
/// Componente base de interaccion por proximidad: detecta cuando un personaje entra o sale de
/// su radio y dispara un evento al pulsar la tecla de uso. No sabe "que hace" la interaccion --
/// eso lo deciden componentes complementarios en el mismo GameObject, suscritos a sus eventos
/// (p.ej. InteractableTeleportModule, InteractableKeyHintModule). Anadir un modulo nuevo no
/// requiere tocar esta clase.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
[ExecuteAlways]
public class Interactable : MonoBehaviour
{
    [Header("Uso")]
    [Tooltip("Tecla para accionar la interaccion estando dentro del radio.")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Radio de activacion")]
    [Tooltip("Radio de activacion (se sincroniza con el CircleCollider2D).")]
    public float activationRadius = 2f;
    [Tooltip("Sincroniza automaticamente con el CircleCollider2D.radius.")]
    public bool syncColliderRadius = true;

    [Header("Filtro de contacto")]
    [Tooltip("Capas que pueden entrar en contacto con este interactuable.")]
    public LayerMask contactLayers = ~0;
    [Tooltip("Ademas de la capa, exige un tag concreto para poder interactuar.")]
    public bool requireTag = true;
    public string requiredTag = "Player";

    [Header("Gizmos (solo visual)")]
    public bool drawGizmos = true;
    public Color areaFillColor = new Color(1f, 1f, 1f, 0.10f);
    public Color areaOutlineColor = new Color(1f, 1f, 1f, 0.9f);

    /// <summary>Se dispara cuando un personaje valido entra en el radio.</summary>
    public event Action<GameObject> OnCharacterEnter;
    /// <summary>Se dispara cuando el personaje que estaba dentro sale del radio.</summary>
    public event Action<GameObject> OnCharacterExit;
    /// <summary>Se dispara al pulsar interactKey mientras hay un personaje dentro del radio.</summary>
    public event Action<GameObject> OnInteract;

    CircleCollider2D _col;
    GameObject _currentTarget;

    void Awake()
    {
        _col = GetComponent<CircleCollider2D>();
        if (_col) _col.isTrigger = true;
    }

    void OnEnable() => SyncCollider();

    void OnValidate()
    {
        if (activationRadius < 0f) activationRadius = 0f;
        SyncCollider();
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            SyncCollider();
            return;
        }

        if (_currentTarget != null && Input.GetKeyDown(interactKey))
            OnInteract?.Invoke(_currentTarget);
    }

    void SyncCollider()
    {
        if (!_col) _col = GetComponent<CircleCollider2D>();
        if (_col && syncColliderRadius)
        {
            _col.isTrigger = true;
            if (_col.radius != activationRadius) _col.radius = activationRadius;
        }
    }

    bool IsValidContact(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & contactLayers) == 0) return false;
        if (requireTag && !other.transform.CompareTag(requiredTag)) return false;
        return true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_currentTarget != null) return;
        if (!IsValidContact(other)) return;

        _currentTarget = other.gameObject;
        OnCharacterEnter?.Invoke(_currentTarget);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject != _currentTarget) return;

        var target = _currentTarget;
        _currentTarget = null;
        OnCharacterExit?.Invoke(target);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        UnityEditor.Handles.color = areaFillColor;
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.forward, activationRadius);
        UnityEditor.Handles.color = areaOutlineColor;
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, activationRadius);
    }
#endif

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(Interactable)), UnityEditor.CanEditMultipleObjects]
    class InteractableEditor : UnityEditor.Editor
    {
        void OnSceneGUI()
        {
            var it = (Interactable)target;

            UnityEditor.EditorGUI.BeginChangeCheck();
            float r = UnityEditor.Handles.RadiusHandle(Quaternion.identity, it.transform.position, it.activationRadius);
            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                UnityEditor.Undo.RecordObject(it, "Change Interactable Radius");
                it.activationRadius = Mathf.Max(0f, r);
                if (it.syncColliderRadius && it.TryGetComponent(out CircleCollider2D col))
                {
                    UnityEditor.Undo.RecordObject(col, "Sync Collider Radius");
                    col.radius = it.activationRadius;
                    col.isTrigger = true;
                    UnityEditor.EditorUtility.SetDirty(col);
                }
                UnityEditor.EditorUtility.SetDirty(it);
            }
        }
    }
#endif
}
