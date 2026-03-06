using System;
using UnityEngine;

/// <summary>
/// Módulo de emote: reproduce (o detiene) un AnimationClip en el portrait
/// del personaje indicado mediante Playables. No gestiona placement ni sprite.
/// El timing respecto a otros módulos se controla con el orden en la lista y el RunMode.
/// </summary>
[Serializable]
public class EmoteModule : DialogueModuleBase
{
    public override string DisplayName => "Emote";

    // --- Perfil ---
    [Tooltip("Perfil del personaje (referencia directa, solo editor).")]
    public CharacterProfile profileRef;

    [Tooltip("ID persistente del perfil (usado en runtime).")]
    public string profileId = "";

    /// <summary>ID efectivo: usa profileRef si está disponible, cae en profileId serializado.</summary>
    public string ProfileId => profileRef != null ? profileRef.ProfileId : profileId;

    // --- Animación ---
    [Tooltip("Si está activo, detiene el emote del personaje en lugar de iniciarlo.")]
    public bool stopAnimation = false;

    [Tooltip("Clip de animación a reproducir.")]
    public AnimationClip clip;

    [Tooltip("Velocidad de reproducción.")]
    public float speed = 1f;

    [Tooltip("Si está activo, la animación se repite en bucle.")]
    public bool loop = false;
}
