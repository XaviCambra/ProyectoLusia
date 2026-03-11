using System;
using UnityEngine;

/// <summary>
/// Pausa silenciosa: detiene la ejecución del ChatRunner durante
/// <see cref="duration"/> segundos antes de continuar al siguiente módulo.
/// Ignorado por DialogueRunner (sin executor registrado).
/// </summary>
[Serializable]
public class ChatPauseModule : DialogueModuleBase
{
    public override string DisplayName => "Chat Pause";

    [Min(0f)]
    [Tooltip("Segundos de pausa antes de continuar.")]
    public float duration = 1f;
}
