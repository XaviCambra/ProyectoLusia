using System.Threading.Tasks;

/// <summary>
/// Extensión opcional de <see cref="IPortraitController"/> que expone
/// puntos de control durante la animación de colocación del retrato.
/// </summary>
public interface IPortraitPlacementMilestones
{
    public readonly struct Milestones
    {
        public readonly Task Mid;      // se completa al ~50% de la colocación
        public readonly Task Complete; // se completa al final de la colocación
        public Milestones(Task mid, Task complete) { Mid = mid; Complete = complete; }
    }

    /// <summary>
    /// Inicia la aplicación visual del módulo de retrato y devuelve Tasks para
    /// esperar a "mitad" y "completado". Este método NO espera a que termine la colocación.
    /// </summary>
    Milestones ApplyWithMilestones(PortraitModule module);
}
