/// <summary>
/// Contrato base para todos los módulos de un nodo de diálogo.
/// Los módulos son unidades de funcionalidad independiente que se
/// ejecutan secuencialmente dentro de un nodo, en el orden que
/// el diseñador establezca.
/// </summary>
public interface IDialogueModule
{
    /// <summary>Nombre mostrado en el editor.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Si es true, el runner espera a que este módulo termine
    /// antes de pasar al siguiente. Si es false, se ejecuta en
    /// paralelo y el runner continúa de inmediato.
    /// </summary>
    bool Blocks { get; set; }
}
