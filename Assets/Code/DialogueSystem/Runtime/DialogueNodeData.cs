// Runtime/LegacySoModel/DialogueNodeData.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum EventPayloadType { None, Int, Float, String, Bool, Char } // Char = string len 1

// === Anim / Placement enums (usados por el nodo) ===
public enum AppearanceMode { Preplaced, SlideIn }
public enum TextStartTiming { BeforeAnimation, AfterAnimation }
public enum Spot
{
    Auto,     // hereda última posición conocida del perfil
    Keep,     // no mover (solo posible fade)
    Left,
    Center,
    Right,
    OffLeft,  // fuera de pantalla por la izquierda
    OffRight  // fuera de pantalla por la derecha
}

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

    public EventPayloadType eventPayloadType = EventPayloadType.None;
    // Valores posibles según el tipo seleccionado
    public int eventInt;
    public float eventFloat;
    public string eventString; // usado también para Char (longitud 1)
    public bool eventBool;

    /// <summary>
    /// Construye el payload final a partir de la configuración del nodo.
    /// </summary>
    public DialogueEventPayload BuildEventPayload()
    {
        return new DialogueEventPayload
        {
            key = eventKey,
            payloadType = eventPayloadType,
            intValue = eventInt,
            floatValue = eventFloat,
            stringValue = eventString,
            boolValue = eventBool,
        };
    }

    [Tooltip("Si está activo, este nodo se usará como punto de inicio del diálogo.")]
    public bool isStart = false;

    [Tooltip("Etiqueta opcional para distinguir entre múltiples nodos de inicio.")]
    public string startId;   // p.ej. "prologo", "capitulo2", "finalA"

    [Header("Posición en el editor")]
    public Rect nodeRect = new Rect(100, 100, 320, 180);

    // ========== EXTRAS / ANIMACIÓN ==========
    [Header("Extras / Animación (plegable)")]
    [Tooltip("Muestra/Oculta los campos extra en el editor")]
    public bool showExtrasBox = false; // Punto 2 (toggle de UI editor)

    [Header("Aparición / Colocación")]
    [Tooltip("Si 'Preplaced' el personaje ya está en pantalla sin animación; si 'SlideIn' se mueve al destino")]
    public AppearanceMode appearance = AppearanceMode.Preplaced;

    [Tooltip("Desde dónde empieza este nodo (Auto = hereda última posición conocida)")]
    public Spot origin = Spot.Auto;

    [Tooltip("Destino del personaje (OffLeft/OffRight = salida por ese lateral)")]
    public Spot target = Spot.Center;

    [Tooltip("Velocidad de desplazamiento (px/seg)")]
    public float moveSpeed = 600f; // Punto 7

    [Tooltip("Cuándo empieza el texto del typewriter")]
    public TextStartTiming textStart = TextStartTiming.AfterAnimation; // Punto 8

    [Header("Fade (entrada/salida)")]
    [Tooltip("Activar desvanecidos al entrar/salir")]
    public bool useFade = true; // Punto 9 (toggle maestro de fade)

    [Range(0, 100)] public int enterFromOpacity = 0;   // 0 = 0%, 100 = 100%
    [Range(0, 100)] public int enterToOpacity = 100;

    [Range(0, 100)] public int exitFromOpacity = 100;
    [Range(0, 100)] public int exitToOpacity = 0;
    // ========== END EXTRAS / ANIMACIÓN ==========

    // ========== SPECIAL ANIMATION (OPCIONAL) ==========
    [Header("Special Animation (opcional)")]
    [Tooltip("Si está activo, el retrato del personaje reproducirá una AnimationClip especial en este nodo.")]
    public bool playSpecialAnimation = false;

    [Tooltip("AnimationClip a reproducir (puede animar RectTransform: anchoredPosition, localScale, etc.).")]
    public AnimationClip specialAnimation;

    [Tooltip("Velocidad con la que reproducir la animación (1 = normal).")]
    public float specialAnimSpeed = 1f;

    [Tooltip("Intentar que la animación sea cíclica. Ideal si el clip tiene loop activado en Import Settings.")]
    public bool specialAnimLoop = true;
    // ========== END SPECIAL ANIMATION ==========

    // ========== TYPEWRITER ==========
    [Header("Typewriter (plegable)")]
    [Tooltip("Muestra/Oculta los campos del typewriter en el editor")]
    public bool showTypewriterBox = false;
    // ========== END TYPEWRITER ==========

    // --- NUEVO: referencia lógica a un perfil ---
    [Header("Perfil (opcional)")]
    [Tooltip("Referencia directa (editor/runtime) al perfil. Si está, tiene prioridad en el editor.")]
    public CharacterProfile profileRef;   // <--- NUEVO
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

        DGLog.Info($"NodeData.OnBeforeSerialize GUID={GUID} profileId='{profileId}'");
    }

    public void OnAfterDeserialize()
    {
        if (string.IsNullOrEmpty(guid))
            guid = Guid.NewGuid().ToString();

        DGLog.Info($"NodeData.OnAfterDeserialize GUID={GUID} profileId='{profileId}'");
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