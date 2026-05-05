using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Contrato que debe implementar cualquier UI de chat.
/// El <see cref="ChatRunner"/> delega toda la presentación aquí,
/// manteniendo el runner libre de dependencias de UI.
/// </summary>
public interface IChatPresenter
{
    /// <summary>Añade una entrada al historial visible (texto, emoji o imagen).</summary>
    void AddMessage(ChatEntry entry);

    /// <summary>
    /// Muestra el indicador "está escribiendo..." del perfil indicado
    /// durante <paramref name="seconds"/> segundos.
    /// </summary>
    Task ShowTypingAsync(CharacterProfile profile, float seconds, CancellationToken ct);

    /// <summary>Presenta las opciones de texto al jugador y devuelve el índice elegido.</summary>
    Task<int> ShowChoicesAsync(IReadOnlyList<ChoiceModule.ChoiceData> choices, CancellationToken ct);

    /// <summary>Presenta las opciones de imagen al jugador y devuelve el índice elegido.</summary>
    Task<int> ShowImageChoicesAsync(IReadOnlyList<ImageChoiceModule.ImageChoiceData> choices, CancellationToken ct);

    /// <summary>Limpia el historial visible (nuevo chat).</summary>
    void Clear();
}
