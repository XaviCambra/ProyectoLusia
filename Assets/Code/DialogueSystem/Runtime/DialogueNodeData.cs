// Runtime/LegacySoModel/DialogueNodeData.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Datos serializables de un nodo de diálogo dentro del asset DialogueGraph.
/// Define la información básica que el diseñador edita en el GraphView.
/// </summary>
[Serializable]
public class DialogueNodeData : ISerializationCallbackReceiver
{
    // Flag maestro: este nodo usa efecto typewriter al mostrarse
    public bool useTypewriter = false;

    // Solo para editor/UI (puede no serializarse si no quieres)
    public bool twShowAdvanced = false;

    // Overrides locales del nodo (subconjunto mínimo)
    public TypewriterOverrides tw = TypewriterOverrides.Default();

    [SerializeField, HideInInspector] private string guid;   // <- ¡serializado no tocar!
    public string GUID => guid;

    // noop: forzar recompilación sin cambiar la lógica

    [Header("Apariencia (Editor)")]
    public int bgColorIndex = 0; // índice en la paleta
    public Color bgColor = new Color(0.16f, 0.16f, 0.20f, 1f); // se mantiene para retrocompat

    // Paleta fija (puedes cambiar/añadir)
    public static readonly (string name, Color color)[] NodePalette = new (string, Color)[]
    {
        ("Gris",    new Color(0.16f, 0.16f, 0.20f, 1f)),
        ("Azul",    new Color(0.18f, 0.24f, 0.32f, 1f)),
        ("Morado",  new Color(0.22f, 0.18f, 0.30f, 1f)),
        ("Verde",   new Color(0.20f, 0.26f, 0.20f, 1f)),
        ("Ambar",   new Color(0.30f, 0.25f, 0.12f, 1f)),
        ("Cian",    new Color(0.16f, 0.28f, 0.30f, 1f)),
    };

    [Header("Contenido del diálogo")]
    public string speakerName;

    [TextArea(3, 8)] 
    public string lineText;

    // --- NUEVO: Soporte de localización ---
    [Header("Localización")]
    [Tooltip("Si está activo, el nodo usará una clave de localización en lugar de texto literal.")]
    public bool localization = false;

    [Tooltip("Clave de localización (por ejemplo: dialogue.intro.hello)")]
    public string locKey;

    [Header("Posición del personaje")]
    public CharacterAnchor anchor = CharacterAnchor.Left;
    public Vector2 customAnchor;

    [Header("Configuración de elección")]
    public bool isChoiceNode;
    [Range(2, 4)] public int choiceCount = 2;
    public List<ChoiceData> choices = new();

    [Header("Eventos del nodo")]
    public string eventKey;
    public UnityEvent onEnter;

    [Tooltip("Si está activo, este nodo se usará como punto de inicio del diálogo.")]
    public bool isStart = false;

    [Header("Posición en el editor")]
    public Rect nodeRect = new Rect(100, 100, 320, 180);

    // --- NUEVO: referencia lógica a un perfil ---
    [Header("Perfil (opcional)")]
    [Tooltip("ID del CharacterProfile almacenado en la base de datos")]
    public string profileId;     // string llano: sin referencia directa al asset
    [Tooltip("Clave del retrato a usar dentro del perfil (PortraitEntry.key)")]
    public string portraitKey;

    public DialogueNodeData()
    {
        if (string.IsNullOrEmpty(guid))
            guid = Guid.NewGuid().ToString();
        onEnter = new UnityEvent();
    }

    public void OnBeforeSerialize()
    {
        if (string.IsNullOrEmpty(guid))
            guid = Guid.NewGuid().ToString();
    }

    public void OnAfterDeserialize()
    {
        if (string.IsNullOrEmpty(guid))
            guid = Guid.NewGuid().ToString();
    }
}

// En DialogueNodeData.cs (o donde declares el modelo del nodo)
[System.Serializable]
public struct TypewriterOverrides
{
    // Subconjunto mínimo y útil (puedes ampliar fácilmente)
    public float secondsPerChar;      // 0.001–0.2
    public float globalSpeed;         // 0.1–3
    public bool respectRichText;      // true/false
    public bool minimalWhitespaceDelay; // true/false

    public float commaPct;            // x multiplicador
    public float periodPct;
    public float ellipsisPct;

    // Fábrica de valores por defecto sensatos (alineados al Profile por defecto)
    public static TypewriterOverrides Default() => new TypewriterOverrides
    {
        secondsPerChar = 0.03f,
        globalSpeed = 1f,
        respectRichText = true,
        minimalWhitespaceDelay = true,
        commaPct = 2.0f,
        periodPct = 3.0f,
        ellipsisPct = 5.0f,
    };
}