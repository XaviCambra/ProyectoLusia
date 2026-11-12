/// <summary>
/// Controla cómo el runner gestiona la ejecución de un módulo
/// en relación con los módulos anteriores y siguientes.
/// </summary>
public enum ModuleRunMode
{
    /// <summary>Arranca y el runner continúa de inmediato. La task no se trackea.</summary>
    FireAndForget,

    /// <summary>
    /// Arranca y el runner continúa de inmediato, pero la task queda trackeada.
    /// El siguiente módulo Blocking esperará a que todos los Parallel pendientes terminen.
    /// </summary>
    Parallel,

    /// <summary>
    /// Primero espera a todos los módulos Parallel pendientes y luego espera este módulo.
    /// Actúa como punto de sincronización.
    /// </summary>
    Blocking,
}

/// <summary>
/// Contrato base para todos los módulos de un nodo de diálogo.
/// Los módulos son unidades de funcionalidad independiente que se
/// ejecutan dentro de un nodo en el orden que el diseñador establezca.
///
/// Quien ejecuta cada modulo es el <see cref="IModuleExecutor"/> de su tipo que este
/// montado en la jerarquia del <see cref="DialogueRunner"/> (hay un solo runner; los
/// "motores" Retratos y Chat son solo distintos juegos de executors). Si el runner no
/// tiene executor para un modulo, ese modulo se salta: DialogueRunner avisa al arrancar
/// el grafo, y el boton Validar del editor lo detecta en los grafos de chat. Los
/// comentarios "// USO:" de cada modulo dicen que motor lo monta hoy.
/// </summary>
public interface IDialogueModule
{
    /// <summary>Nombre mostrado en el editor.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Controla cuándo arranca este módulo y si el runner espera a que termine
    /// antes de pasar al siguiente.
    /// </summary>
    ModuleRunMode RunMode { get; set; }
}
