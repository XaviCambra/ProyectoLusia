using System;
using UnityEngine;

[Serializable]
public class PortraitEntry
{
    [Tooltip("Nombre o clave del retrato (ej. 'Default', 'Angry', 'Happy').")]
    public string key;

    [Tooltip("Sprite asociado a la clave anterior.")]
    public Sprite sprite;
}
