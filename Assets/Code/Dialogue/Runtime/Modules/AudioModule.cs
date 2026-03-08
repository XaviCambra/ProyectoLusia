using System;
using UnityEngine;

/// <summary>
/// Módulo de audio: reproduce un AudioClip cuando el nodo llega
/// a este módulo. Puede configurarse como bloqueante (espera a que
/// termine el clip) o no bloqueante (fire and forget).
/// </summary>
[Serializable]
public class AudioModule : DialogueModuleBase
{
    public override string DisplayName => "Audio";

    public AudioModule()
    {
        runMode = ModuleRunMode.FireAndForget; // Por defecto no bloquea
    }

    [Tooltip("Clip de audio a reproducir.")]
    public AudioClip clip;

    [Range(0f, 1f)]
    [Tooltip("Volumen de reproducción.")]
    public float volume = 1f;

    [Tooltip("Si está activo, el módulo espera a que el clip termine antes de continuar (equivale a blocks = true).")]
    public bool waitForCompletion = false;
}
