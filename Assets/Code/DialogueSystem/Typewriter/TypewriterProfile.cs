using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Typewriter Profile", fileName = "NewTypewriterProfile")]
public class TypewriterProfile : ScriptableObject
{
    [Header("Velocidad base")]
    [Range(0.001f, 0.2f)] public float secondsPerChar = 0.03f;
    [Range(0.1f, 3f)] public float globalSpeed = 1f;

    [Header("Opciones")]
    public bool respectRichText = true;
    public bool minimalWhitespaceDelay = true;

    [Header("Multiplicadores de pausa (x tiempo base)")]
    public float commaPct = 2.0f;
    public float periodPct = 3.0f;
    public float ellipsisPct = 5.0f;
    public float questionPct = 3.0f;
    public float exclaimPct = 3.0f;
    public float semicolonPct = 2.5f;
    public float colonPct = 2.5f;
    public float quotePct = 1.5f;
    public float parenPct = 1.5f;
}
