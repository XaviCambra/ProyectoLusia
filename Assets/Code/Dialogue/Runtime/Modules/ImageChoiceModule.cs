using System;
using System.Collections.Generic;
using UnityEngine;

// USO: Solo sistema de Chat Bubble. Ignorado por DialogueRunner.
/// <summary>
/// Módulo de opciones con imagen: presenta al jugador una cuadrícula de sprites
/// como respuesta en el chat. Cada opción navega a un puerto de salida distinto.
/// </summary>
[Serializable]
public class ImageChoiceModule : DialogueModuleBase
{
    [Serializable]
    public class ImageChoiceData
    {
        public Sprite sprite;
        public string portName;
    }

    public override string DisplayName => "Image Choice";

    public List<ImageChoiceData> choices = new();
}
