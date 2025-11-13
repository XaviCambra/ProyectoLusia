// Runtime/Dialogue/DialogueNodeData.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueNodeData
{
    // ---------- Identidad / Layout ----------
    public string GUID = Guid.NewGuid().ToString();
    public Rect nodeRect = new Rect(100, 100, 480, 200);

    // ---------- Apariencia del nodo (Editor) ----------
    [Serializable]
    public struct PaletteEntry { public string name; public Color color; }
    // Personaliza esta paleta en tu proyecto si quieres más opciones
    public static readonly List<PaletteEntry> NodePalette = new()
    {
        new PaletteEntry { name = "Graphite",       color = new Color(0.15f, 0.17f, 0.19f, 1f) },
        new PaletteEntry { name = "Ash Gray",       color = new Color(0.25f, 0.27f, 0.30f, 1f) },
        new PaletteEntry { name = "Deep Coffee",    color = new Color(0.32f, 0.24f, 0.20f, 1f) },
        new PaletteEntry { name = "Rust Brown",     color = new Color(0.48f, 0.28f, 0.22f, 1f) },
        new PaletteEntry { name = "Faded Clay",     color = new Color(0.52f, 0.33f, 0.30f, 1f) },
        new PaletteEntry { name = "Forest Olive",   color = new Color(0.30f, 0.40f, 0.28f, 1f) },
        new PaletteEntry { name = "Deep Moss",      color = new Color(0.26f, 0.35f, 0.30f, 1f) },
        new PaletteEntry { name = "Muted Teal",     color = new Color(0.25f, 0.43f, 0.45f, 1f) },
        new PaletteEntry { name = "Storm Blue",     color = new Color(0.25f, 0.35f, 0.48f, 1f) },
        new PaletteEntry { name = "Indigo Night",   color = new Color(0.23f, 0.30f, 0.46f, 1f) },
        new PaletteEntry { name = "Royal Plum",     color = new Color(0.32f, 0.24f, 0.40f, 1f) },
        new PaletteEntry { name = "Deep Lavender",  color = new Color(0.38f, 0.29f, 0.44f, 1f) },
        new PaletteEntry { name = "Midnight Cyan",  color = new Color(0.20f, 0.35f, 0.40f, 1f) },
        new PaletteEntry { name = "Dusty Burgundy", color = new Color(0.40f, 0.25f, 0.30f, 1f) },
        new PaletteEntry { name = "Smoky Navy",     color = new Color(0.18f, 0.25f, 0.36f, 1f) },
        new PaletteEntry { name = "Obsidian Green", color = new Color(0.18f, 0.28f, 0.23f, 1f) }
    };

    [Range(0, 99)] public int bgColorIndex = 0;
    public Color bgColor = new Color(0.22f, 0.22f, 0.22f, 1f);

    // ---------- Perfil / Retrato ----------
    public CharacterProfile profileRef;            // Referencia directa (Editor)
    public string profileId;             // Id persistente (Runtime/rehidratación)
    public string portraitKey;           // Clave de sprite dentro del perfil

    // ---------- Texto / Localización ----------
    public string speakerName = "";
    public bool localization = false;            // Si true: usar locKey
    [TextArea(2, 6)]
    public string lineText = "";
    public string locKey = "";

    // ---------- Aparición / Movimiento ----------
    public AppearanceMode appearance = AppearanceMode.Cut;
    public Spot origin = Spot.LeftOffscreen;
    public Spot target = Spot.Left;
    public float moveSpeed = 600f;

    public TextStartTiming textStart = TextStartTiming.OnEnterComplete;

    // ---------- Fade ----------
    public bool useFade = false;
    [Range(0, 100)] public int enterFromOpacity = 0;
    [Range(0, 100)] public int enterToOpacity = 100;
    //[Range(0, 100)] public int exitFromOpacity = 100;
    //[Range(0, 100)] public int exitToOpacity = 0;

    // ---------- Animación especial ----------
    public bool playSpecialAnimation = false;
    public AnimationClip specialAnimation;
    public float specialAnimSpeed = 1f;
    public bool specialAnimLoop = false;
    public SpecialStartTiming specialStart = SpecialStartTiming.WithPlacementComplete;

    // ---------- Typewriter ----------
    [Serializable]
    public struct TypewriterSettings
    {
        [Range(0.001f, 0.2f)] public float secondsPerChar;
        [Range(0.1f, 3f)] public float globalSpeed;
    }

    public bool showTypewriterBox = false; // plegable (Editor)
    public bool useTypewriter = false;
    public TypewriterSettings tw = new TypewriterSettings
    {
        secondsPerChar = 0.03f,
        globalSpeed = 1f,
    };

    // ---------- Pliegues UI (Editor) ----------
    public bool showExtrasBox = false;

    // ---------- Inicio / Elecciones ----------
    public bool isStart = false;
    public string startId = "";        // identificador externo cuando es nodo de inicio

    public bool isChoiceNode = false;
    [Range(2, 4)] public int choiceCount = 2;

    // --- UI de opciones ---
    [Header("Opciones (UI)")]
    public bool showBlockedChoices = false; // false = ocultar bloqueadas (por defecto). true = mostrarlas deshabilitadas.

    [Serializable]
    public class ChoiceData
    {
        public string choiceText = "Opción";
        public string portName = "out";

        // --- Localización (por opción) ---
        public bool choiceUseLocalization = false; // si true: usar choiceLocKey
        public string choiceLocKey = "";           // clave de localización

        // --- Afinidad (requisitos por opción) ---
        public bool requiresAffinity = false;
        public string affinityKey = "";
        public float requiredAffinity = 0f;
        public bool invertRequirement = false;

        // --- Progreso (requisito complementario por opción) ---
        public bool requiresProgress = false;
        public string progressMethod = "";
        public ProgressArgType progressArgType = ProgressArgType.None;
        public int progressArgInt = 0;
        public float progressArgFloat = 0f;
        public string progressArgString = "";
    }
    public List<ChoiceData> choices = new();

    // ---------- Eventos ----------
    public string eventKey = "";
    public EventPayloadType eventPayloadType = EventPayloadType.None;
    public int eventInt = 0;
    public float eventFloat = 0f;
    public string eventString = "";  // también se usa para 'Char' (longitud 0..1)
    public bool eventBool = false;

    // ---------- Utilidad ----------
    public DialogueEventPayload BuildEventPayload()
    {
        var payload = new DialogueEventPayload
        {
            key = eventKey,
            type = eventPayloadType
        };

        switch (eventPayloadType)
        {
            case EventPayloadType.None:
                // no payload
                break;
            case EventPayloadType.Int: payload.intValue = eventInt; break;
            case EventPayloadType.Float: payload.floatValue = eventFloat; break;
            case EventPayloadType.String: payload.stringValue = eventString ?? string.Empty; break;
            case EventPayloadType.Bool: payload.boolValue = eventBool; break;
            case EventPayloadType.Char:
                payload.charValue = !string.IsNullOrEmpty(eventString) ? eventString[0] : '\0';
                break;
        }
        return payload;
    }

    // ---------- Saneo mínimo ----------
    public void OnValidate()
    {
        // Paleta
        if (NodePalette != null && NodePalette.Count > 0)
        {
            bgColorIndex = Mathf.Clamp(bgColorIndex, 0, NodePalette.Count - 1);
            bgColor = NodePalette[bgColorIndex].color;
        }

        // Clamp básicos
        moveSpeed = Mathf.Max(0f, moveSpeed);

        enterFromOpacity = Mathf.Clamp(enterFromOpacity, 0, 100);
        enterToOpacity = Mathf.Clamp(enterToOpacity, 0, 100);
        //exitFromOpacity = Mathf.Clamp(exitFromOpacity, 0, 100);
        //exitToOpacity = Mathf.Clamp(exitToOpacity, 0, 100);

        specialAnimSpeed = Mathf.Max(0f, specialAnimSpeed);

        choiceCount = Mathf.Clamp(choiceCount, 2, 4);
        // Garantiza que eventString no exceda 1 char cuando el tipo es Char
        if (eventPayloadType == EventPayloadType.Char && !string.IsNullOrEmpty(eventString) && eventString.Length > 1)
            eventString = eventString.Substring(0, 1);
    }
}

// ========================= Enums usadas por la vista =========================
public enum AppearanceMode { Cut, Slide, None }
public enum Spot
{
    LeftOffscreen, Left, CenterLeft, Center, CenterRight, Right, RightOffscreen
}
public enum TextStartTiming { OnEnterStart, OnEnterMid, OnEnterComplete }

public enum EventPayloadType { None, Int, Float, String, Bool, Char }

public enum ProgressArgType { None, Int, Float, String }

public enum SpecialStartTiming { WithPlacementStart, WithPlacementComplete, WithTextStart, Immediate }