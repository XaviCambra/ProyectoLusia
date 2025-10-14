// Runtime/LegacySoModel/ChoiceData.cs
using System;

/// <summary>
/// Representa una opción de diálogo dentro de un nodo de elección.
/// Cada opción tiene un texto visible y un identificador de puerto único
/// que se usa para conectar con otros nodos del diálogo.
/// </summary>
[Serializable]
public class ChoiceData
{
    /// <summary>Texto que verá el jugador en la elección.</summary>
    public string choiceText;

    /// <summary>Nombre interno del puerto de salida asociado a esta opción.</summary>
    public string portName;
}
