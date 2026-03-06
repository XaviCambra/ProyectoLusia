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
    [Tooltip("Clip de animación a reproducir. Sin clip asignado, detiene el emote activo del personaje.")]
    public AnimationClip clip;

    [Tooltip("Velocidad de reproducción.")]
    public float speed = 1f;

    [Tooltip("Si está activo, la animación se repite en bucle.")]
    public bool loop = false;

    [Tooltip("Si está activo, la animación no se interrumpe al cambiar de nodo. Se detiene con otro EmoteModule sin clip.")]
    public bool persistent = false;
}
