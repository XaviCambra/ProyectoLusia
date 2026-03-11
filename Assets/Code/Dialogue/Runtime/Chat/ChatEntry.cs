/// <summary>
/// Entrada inmutable del historial de chat.
/// Contiene el nombre del hablante y el texto tal como aparecen en el grafo.
/// </summary>
public readonly struct ChatEntry
{
    public readonly string speakerName;
    public readonly string text;
    public readonly bool   isOwn;

    public ChatEntry(string speakerName, string text, bool isOwn = false)
    {
        this.speakerName = speakerName ?? string.Empty;
        this.text        = text        ?? string.Empty;
        this.isOwn       = isOwn;
    }
}
