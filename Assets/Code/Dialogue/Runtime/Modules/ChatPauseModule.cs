using System;
using UnityEngine;

// USO: Solo escenas de Chat (requiere ChatPauseModuleExecutor). Sin efecto si no hay executor para este modulo en la escena.
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
