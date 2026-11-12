using System;
using UnityEngine;

// USO: Solo Chat (WaitForSignalModuleExecutor). Retratos no lo monta: ahi se salta. Ver IDialogueModule.
/// <summary>
/// Detiene la ejecución indefinidamente hasta que alguien llame a
/// <see cref="SignalSO.Raise"/> en la señal configurada.
/// Útil para sincronizar el chat con eventos externos del juego.
/// </summary>
[Serializable]
public class WaitForSignalModule : DialogueModuleBase
{
    public override string DisplayName => "Wait For Signal";

    [Tooltip("Señal que debe dispararse para que la ejecución continúe.")]
    public SignalSO signal;
}
