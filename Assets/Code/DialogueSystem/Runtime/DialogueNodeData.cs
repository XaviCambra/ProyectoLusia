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
public class DialogueNodeData
{
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
    [TextArea(3, 8)] public string lineText;

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
