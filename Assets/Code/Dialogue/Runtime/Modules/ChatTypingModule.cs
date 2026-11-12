using System;
using UnityEngine;

// USO: Solo Chat (ChatTypingModuleExecutor). Retratos no lo monta: ahi se salta. Ver IDialogueModule.
/// <summary>
/// Muestra el indicador "está escribiendo..." de un personaje durante
/// <see cref="duration"/> segundos antes de continuar.
/// </summary>
[Serializable]
public class ChatTypingModule : DialogueModuleBase
{
    public override string DisplayName => "Chat Typing";

    [Min(0f)]
    [Tooltip("Duración en segundos del indicador 'está escribiendo'.")]
    public float duration = 2f;
}
