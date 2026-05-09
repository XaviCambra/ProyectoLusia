using System;
using UnityEngine;

// USO: Solo sistema de Chat Bubble. Sin executor implementado (módulo reservado para uso futuro).
/// <summary>
/// Detiene la ejecución indefinidamente hasta que alguien llame a
/// <see cref="SignalSO.Raise"/> en la señal configurada.
/// Útil para sincronizar el chat con eventos externos del juego.
/// Ignorado por DialogueRunner (sin executor registrado).
/// </summary>
[Serializable]
public class WaitForSignalModule : DialogueModuleBase
{
    public override string DisplayName => "Wait For Signal";

    [Tooltip("Señal que debe dispararse para que la ejecución continúe.")]
    public SignalSO signal;
}
