using System;

/// <summary>
/// Contrato extendido para el executor de opciones de elección.
/// Añade el evento de selección a <see cref="IModuleExecutor"/> para que
/// <see cref="DialogueRunner"/> pueda navegar al nodo correcto sin
/// depender de la clase concreta <see cref="ChoiceModuleExecutor"/> (DIP).
/// </summary>
public interface IChoiceExecutor : IModuleExecutor
{
    /// <summary>
    /// Se invoca cuando el jugador selecciona una opción.
    /// El parámetro es el nombre del puerto de salida elegido.
    /// </summary>
    event Action<string> OnChoiceSelected;
}
