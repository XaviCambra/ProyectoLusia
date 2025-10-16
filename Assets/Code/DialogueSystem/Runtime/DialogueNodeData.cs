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
    [SerializeField] private string guid;  // Identificador único (no editable)
    public string GUID => guid;

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
        guid = System.Guid.NewGuid().ToString();
        onEnter = new UnityEvent();
    }
}
