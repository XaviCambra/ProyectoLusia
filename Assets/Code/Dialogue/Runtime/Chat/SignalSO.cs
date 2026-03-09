using System;
using UnityEngine;

/// <summary>
/// Canal de evento reutilizable basado en ScriptableObject.
/// Cualquier sistema puede suscribirse a <see cref="OnRaised"/> o llamar
/// a <see cref="Raise"/> para señalizar sin acoplar emisor y receptor.
/// </summary>
[CreateAssetMenu(menuName = "Dialogue/Signal", fileName = "NewSignal")]
public class SignalSO : ScriptableObject
{
    public event Action OnRaised;

    /// <summary>Emite la señal y notifica a todos los suscriptores.</summary>
    public void Raise() => OnRaised?.Invoke();
}
