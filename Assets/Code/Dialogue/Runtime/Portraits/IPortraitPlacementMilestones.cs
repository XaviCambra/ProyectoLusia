using System.Threading.Tasks;

public interface IPortraitPlacementMilestones
{
    public readonly struct Milestones
    {
        public readonly Task Mid;       // se completa al ~50% de la colocación
        public readonly Task Complete;  // se completa al final de la colocación
        public Milestones(Task mid, Task complete) { Mid = mid; Complete = complete; }
    }

    /// <summary>
    /// Inicia la aplicación visual del nodo (sprite, tintes, escalas y colocación)
    /// y devuelve Tasks para esperar a "mitad" y "completado".
    /// Importante: este método NO espera a que termine la colocación.
    /// </summary>
    Milestones ApplyWithMilestones(DialogueNodeData node);
}
