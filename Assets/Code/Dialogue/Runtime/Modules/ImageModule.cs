using System;
using UnityEngine;

// USO: Solo sistema de Chat Bubble. Ignorado por DialogueRunner.
/// <summary>
/// Módulo de imagen: muestra un sprite enmarcado en el chat, más grande que un emoji.
/// A diferencia de EmojiModule, el prefab asociado incluye un marco visual alrededor de la imagen.
/// </summary>
[Serializable]
public class ImageModule : DialogueModuleBase
{
    public override string DisplayName => "Image";

    [Tooltip("Sprite de la imagen a mostrar en el chat.")]
    public Sprite image;

    [Tooltip("Si está activo, la imagen aparece en el lado derecho (mensaje propio).")]
    public bool isOwn = false;
}
