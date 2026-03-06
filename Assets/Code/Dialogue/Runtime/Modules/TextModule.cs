using System;
using UnityEngine;

/// <summary>
/// Módulo de texto: muestra el nombre del hablante y el cuerpo del diálogo,
/// opcionalmente con efecto typewriter y soporte de localización.
/// </summary>
[Serializable]
public class TextModule : DialogueModuleBase
{
    public override string DisplayName => "Text";

    [Tooltip("Nombre del personaje que habla. Puede estar vacío para narración.")]
    public string speakerName = "";

    [Tooltip("Si está activo, se usa la clave de localización en lugar del texto literal.")]
    public bool useLocalization = false;

    [TextArea(2, 6)]
    [Tooltip("Texto literal del diálogo.")]
    public string text = "";

    [Tooltip("Clave de localización para obtener el texto en el idioma activo.")]
    public string locKey = "";

    [Tooltip("Si está activo, el texto se revela carácter a carácter.")]
    public bool useTypewriter = true;

    [Range(0.001f, 0.2f)]
    [Tooltip("Segundos entre cada carácter revelado.")]
    public float secondsPerChar = 0.03f;
}
