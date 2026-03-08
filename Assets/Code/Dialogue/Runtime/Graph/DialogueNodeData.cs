using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Contenedor mínimo de un nodo de diálogo.
/// Toda la funcionalidad reside en los módulos que contiene.
/// </summary>
[Serializable]
public class DialogueNodeData
{
    // ---------- Identidad / Layout ----------
    public string GUID = Guid.NewGuid().ToString();
    public Rect nodeRect = new Rect(100, 100, 480, 200);

    // ---------- Apariencia del nodo (Editor) ----------
    [Serializable]
    public struct PaletteEntry { public string name; public Color color; }

    public static readonly List<PaletteEntry> NodePalette = new()
    {
        new PaletteEntry { name = "Graphite",       color = new Color(0.22f, 0.23f, 0.25f, 1f) },
        new PaletteEntry { name = "Deep Coffee",    color = new Color(0.32f, 0.24f, 0.20f, 1f) },
        new PaletteEntry { name = "Rust Brown",     color = new Color(0.48f, 0.28f, 0.22f, 1f) },
        new PaletteEntry { name = "Forest Olive",   color = new Color(0.30f, 0.40f, 0.28f, 1f) },
        new PaletteEntry { name = "Muted Teal",     color = new Color(0.25f, 0.43f, 0.45f, 1f) },
        new PaletteEntry { name = "Storm Blue",     color = new Color(0.25f, 0.35f, 0.48f, 1f) },
        new PaletteEntry { name = "Smoky Navy",     color = new Color(0.18f, 0.25f, 0.36f, 1f) },
        new PaletteEntry { name = "Royal Plum",     color = new Color(0.32f, 0.24f, 0.40f, 1f) },
        new PaletteEntry { name = "Dusty Burgundy", color = new Color(0.40f, 0.25f, 0.30f, 1f) },
        new PaletteEntry { name = "Obsidian Green", color = new Color(0.18f, 0.28f, 0.23f, 1f) },
    };

    [Range(0, 99)] public int bgColorIndex = 0;
    public Color bgColor = new Color(0.22f, 0.23f, 0.25f, 1f);

    // ---------- Nodo de inicio ----------
    public bool isStart = false;
    public string startId = "";

    // ---------- Módulos ----------
    [SerializeReference]
    public List<IDialogueModule> modules = new();

    // ---------- Helpers ----------

    /// <summary>Devuelve el primer ChoiceModule del nodo, o null si no tiene.</summary>
    public ChoiceModule GetChoiceModule() =>
        modules?.OfType<ChoiceModule>().FirstOrDefault();

    /// <summary>True si el nodo contiene un ChoiceModule.</summary>
    public bool IsChoiceNode => GetChoiceModule() != null;

    // ---------- Saneo mínimo ----------
    public void OnValidate()
    {
        if (NodePalette != null && NodePalette.Count > 0)
        {
            bgColorIndex = Mathf.Clamp(bgColorIndex, 0, NodePalette.Count - 1);
            bgColor = NodePalette[bgColorIndex].color;
        }
    }
}

// ========================= Enums globales del sistema de diálogo =========================

public enum AppearanceMode   { Cut, Slide, None }
public enum Spot             { LeftOffscreen, Left, CenterLeft, Center, CenterRight, Right, RightOffscreen }
public enum EventPayloadType { None, Int, Float, String, Bool, Char }
public enum ProgressArgType  { None, Int, Float, String }
