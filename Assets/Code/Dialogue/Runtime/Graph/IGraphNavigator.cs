/// <summary>
/// Navega por el grafo de diálogo: encuentra el nodo inicial y el siguiente nodo
/// dada una arista de salida. La resolución de texto y eventos es responsabilidad
/// de los módulos y sus executors.
/// </summary>
public interface IGraphNavigator
{
    void Init(DialogueGraph graph);
    DialogueNodeData StartNode();
    DialogueNodeData NextFrom(DialogueNodeData node, string portName = "Next");
}
