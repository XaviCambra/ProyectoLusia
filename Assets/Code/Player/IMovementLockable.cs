/// <summary>
/// Lo implementa cualquier controlador de movimiento que se pueda apagar/encender temporalmente
/// (ej. mientras hay un dialogo en curso). Quien bloquea el movimiento no necesita conocer el
/// controlador concreto (SimpleWalker2D u otro que llegue mas adelante).
/// </summary>
public interface IMovementLockable
{
    void SetMovementLocked(bool locked);
}
