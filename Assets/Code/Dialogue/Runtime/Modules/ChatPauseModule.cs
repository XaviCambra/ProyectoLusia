using System;
using UnityEngine;

// USO: Solo Chat (ChatPauseModuleExecutor). Retratos no lo monta: ahi se salta. Ver IDialogueModule.
/// <summary>
/// Pausa silenciosa: detiene la ejecución del nodo durante
/// <see cref="duration"/> segundos antes de continuar al siguiente módulo.
/// </summary>
[Serializable]
public class ChatPauseModule : DialogueModuleBase
{
    public override string DisplayName => "Chat Pause";

    [Min(0f)]
    [Tooltip("Segundos de pausa antes de continuar.")]
    public float duration = 1f;
}
