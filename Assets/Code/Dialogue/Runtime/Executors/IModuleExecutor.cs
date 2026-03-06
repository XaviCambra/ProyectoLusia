using System;
using System.Threading.Tasks;

/// <summary>
/// Contrato para los executors de módulos de diálogo.
/// Cada tipo de módulo (<see cref="IDialogueModule"/>) tiene un executor
/// correspondiente que sabe cómo ejecutar ese módulo en runtime.
/// </summary>
public interface IModuleExecutor
{
    /// <summary>Tipo concreto de módulo que este executor maneja.</summary>
    Type ModuleType { get; }

    /// <summary>
    /// Inicialización al comienzo del diálogo.
    /// Solo los executors que lo necesitan (ej. Portrait) implementan lógica aquí.
    /// </summary>
    void Initialize(DialogueGraph graph);

    /// <summary>Llamado al inicio de cada nodo, antes de ejecutar los módulos.</summary>
    void OnNodeBegin();

    /// <summary>
    /// Ejecuta el módulo. Si el módulo es bloqueante, el caller espera a que la Task complete.
    /// Si no es bloqueante, el caller puede ignorar el resultado (fire and forget).
    /// </summary>
    Task ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx);

    /// <summary>Cancela cualquier operación en curso (ej. al navegar al siguiente nodo).</summary>
    void Cancel();

    /// <summary>
    /// Intenta completar/saltar la operación actual anticipadamente (ej. skip typewriter).
    /// Devuelve <c>true</c> si había algo que completar (el caller NO debe avanzar de nodo aún).
    /// Devuelve <c>false</c> si no había operación activa.
    /// </summary>
    bool TryFastForward();

    /// <summary>
    /// Limpieza al finalizar el diálogo completo.
    /// Los executors que gestionan recursos persistentes (retratos, UI) los ocultan/liberan aquí.
    /// </summary>
    void ResetAll();
}
