using System;
using UnityEngine;

/// <summary>
/// Gestiona la lectura de teclas para el sistema de diálogo.
/// Separa el input de la lógica de ejecución del <see cref="DialogueRunner"/> (SRP).
/// Suscribirse a <see cref="OnAdvance"/> / <see cref="OnGameplayAdvance"/>
/// en lugar de detectar teclas directamente en el runner.
/// </summary>
[DisallowMultipleComponent]
public sealed class DialogueInputController : MonoBehaviour
{
    [SerializeField] private KeyCode advanceKey         = KeyCode.N;
    [SerializeField] private KeyCode gameplayAdvanceKey = KeyCode.M;

    /// <summary>Pulsación de la tecla de avance principal (skip typewriter / avanzar nodo).</summary>
    public event Action OnAdvance;

    /// <summary>Pulsación de la tecla de avance de gameplay (avanza nodo sin intentar skip).</summary>
    public event Action OnGameplayAdvance;

    private void Update()
    {
        if (Input.GetKeyDown(advanceKey))
            OnAdvance?.Invoke();

        if (Input.GetKeyDown(gameplayAdvanceKey))
            OnGameplayAdvance?.Invoke();
    }
}
