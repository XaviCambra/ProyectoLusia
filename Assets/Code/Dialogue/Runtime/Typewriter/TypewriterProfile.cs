using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Typewriter Profile", fileName = "NewTypewriterProfile")]
public class TypewriterProfile : ScriptableObject
{
    [Header("Velocidad base")]
    [Range(0.001f, 0.2f)] public float secondsPerChar = 0.03f;

    [Header("Opciones")]
    public bool respectRichText = true;

    [Header("Multiplicadores de pausa (x tiempo base)")]
    [Tooltip("Coma.")] public float commaPct = 2.0f;
    [Tooltip("Punto, cierre de interrogacion y exclamacion.")] public float periodPct = 3.0f;
    [Tooltip("Puntos suspensivos (...)")] public float ellipsisPct = 5.0f;
    [Tooltip("Dos puntos y punto y coma.")] public float colonPct = 2.5f;
    [Tooltip("Comillas, parentesis y corchetes.")] public float bracketPct = 1.5f;
    [Tooltip("Espacio y salto de linea.")] public float whitespacePct = 1.0f;
}
