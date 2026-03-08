using System;
using UnityEngine;

/// <summary>
/// Módulo de retrato: gestiona la aparición, posición, animación y
/// efecto de fade del retrato de un personaje en pantalla.
/// </summary>
[Serializable]
public class PortraitModule : DialogueModuleBase
{
    public override string DisplayName => "Portrait";

    // --- Perfil ---
    [Tooltip("Perfil del personaje.")]
    public CharacterProfile profileRef;

    [Tooltip("Clave del sprite dentro del perfil del personaje.")]
    public string portraitKey = "";

    // --- Apariencia ---
    [Tooltip("Modo de aparición del retrato.")]
    public AppearanceMode appearance = AppearanceMode.Cut;

    [Tooltip("Posición inicial del retrato (antes de animarse).")]
    public Spot origin = Spot.LeftOffscreen;

    [Tooltip("Posición final del retrato (donde se detiene).")]
    public Spot target = Spot.Left;

    [Tooltip("Velocidad de movimiento en píxeles por segundo.")]
    public float moveSpeed = 600f;

    // --- Fade ---
    [Tooltip("Si está activo, el retrato hace fade al aparecer.")]
    public bool useFade = false;

    [Range(0, 100)]
    [Tooltip("Opacidad inicial del fade (0 = transparente, 100 = opaco).")]
    public int enterFromOpacity = 0;

    [Range(0, 100)]
    [Tooltip("Opacidad final del fade.")]
    public int enterToOpacity = 100;

}
