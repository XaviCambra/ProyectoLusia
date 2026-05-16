using UnityEngine;

/// <summary>
/// Contrato para cualquier aplicación del móvil.
/// Implementar en un <see cref="PhoneAppBase"/> para registrarse automáticamente.
/// </summary>
public interface IPhoneApp
{
    string AppName { get; }
    Sprite AppIcon  { get; }
    void OnOpen();
    void OnClose();
}
