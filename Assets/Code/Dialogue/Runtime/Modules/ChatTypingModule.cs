using System;
using UnityEngine;

// USO: Solo sistema de Chat Bubble. Ignorado por DialogueRunner.
/// <summary>
/// Muestra el indicador "está escribiendo..." de un personaje durante
/// <see cref="duration"/> segundos antes de continuar.
/// Ignorado por DialogueRunner (sin executor registrado).
/// </summary>
[Serializable]
public class ChatTypingModule : DialogueModuleBase
{
    public override string DisplayName => "Chat Typing";

    [Tooltip("Personaje cuyo indicador de escritura se muestra.")]
    public CharacterProfile profile;

    [Min(0f)]
    [Tooltip("Duración en segundos del indicador 'está escribiendo'.")]
    public float duration = 2f;
}
