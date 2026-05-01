using System;
using UnityEngine;

// USO: Solo sistema de Chat Bubble. Ignorado por DialogueRunner.
/// <summary>
/// Módulo de emoji: muestra un sprite en el chat sin fondo de burbuja de texto.
/// La imagen se presenta directamente, sin marco ni etiqueta de hablante.
/// </summary>
[Serializable]
public class EmojiModule : DialogueModuleBase
{
    public override string DisplayName => "Emoji";

    [Tooltip("Sprite del emoji o emoticono a mostrar en el chat.")]
    public Sprite emoji;

    [Tooltip("Si está activo, el emoji aparece en el lado derecho (mensaje propio).")]
    public bool isOwn = false;
}
