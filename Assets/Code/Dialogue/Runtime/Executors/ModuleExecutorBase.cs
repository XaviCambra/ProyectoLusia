using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Clase base para todos los executors de módulos de diálogo.
/// Proporciona implementaciones vacías por defecto de <see cref="IModuleExecutor"/>
/// para que las subclases solo sobreescriban los métodos que realmente necesitan (ISP).
/// </summary>
public abstract class ModuleExecutorBase : MonoBehaviour, IModuleExecutor
{
    public abstract Type ModuleType { get; }
    public abstract Task<string> ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx);

    public virtual void Initialize(DialogueGraph graph) { }
    public virtual void OnNodeBegin()                   { }
    public virtual void Cancel()                        { }
    public virtual bool TryFastForward()                => false;
    public virtual void ResetAll()                      { }
}
