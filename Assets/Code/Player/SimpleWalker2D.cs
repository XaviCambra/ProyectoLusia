using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SimpleWalker2D : MonoBehaviour, IMovementLockable
{
    [Header("Movimiento")]
    [Tooltip("Velocidad horizontal en unidades/segundo")]
    public float moveSpeed = 5f;

    [Tooltip("Usar flipX del SpriteRenderer en vez de escalar el transform.")]
    public bool flipSpriteOnDirection = true;

    [Header("Entrada")]
    public bool useStrictAD = false;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private float _inputX;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();

        // Ajustes recomendados para evitar jitter:
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate; // CLAVE
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.freezeRotation = true; // equivale a Freeze Rotation Z en el inspector
    }

    void Update()
    {
        if (useStrictAD)
        {
            bool left = Input.GetKey(KeyCode.A);
            bool right = Input.GetKey(KeyCode.D);
            _inputX = (right ? 1f : 0f) + (left ? -1f : 0f);
        }
        else
        {
            _inputX = Input.GetAxisRaw("Horizontal");
        }

        // Normaliza a -1, 0, 1
        if (_inputX > 0.1f) _inputX = 1f;
        else if (_inputX < -0.1f) _inputX = -1f;
        else _inputX = 0f;

        if (flipSpriteOnDirection && Mathf.Abs(_inputX) > 0.01f)
            _sr.flipX = _inputX < 0f;
    }

    void FixedUpdate()
    {
        // Mantiene Y (gravedad) y sólo cambia X
        Vector2 v = _rb.linearVelocity;
        v.x = _inputX * moveSpeed;
        _rb.linearVelocity = v;
    }

    /// <summary>
    /// Al bloquear, primero para al personaje en seco (input y velocidad horizontal a cero) y
    /// luego apaga el script -- si solo se apagara, la ultima velocidad aplicada se quedaria
    /// arrastrando al personaje mientras Update/FixedUpdate dejan de correr.
    /// </summary>
    public void SetMovementLocked(bool locked)
    {
        if (locked)
        {
            _inputX = 0f;
            Vector2 v = _rb.linearVelocity;
            v.x = 0f;
            _rb.linearVelocity = v;
        }

        enabled = !locked;
    }
}
