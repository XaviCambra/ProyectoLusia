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
    /// <summary>Añade un mensaje al historial visible.</summary>
    void AddMessage(ChatEntry entry);

    /// <summary>
    /// Muestra el indicador "está escribiendo..." del perfil indicado
    /// durante <paramref name="seconds"/> segundos.
    /// </summary>
    Task ShowTypingAsync(CharacterProfile profile, float seconds, CancellationToken ct);

    /// <summary>
    /// Presenta las opciones al jugador y devuelve el índice elegido.
    /// </summary>
    Task<int> ShowChoicesAsync(IReadOnlyList<ChoiceModule.ChoiceData> choices, CancellationToken ct);

    /// <summary>Limpia el historial visible (nuevo chat).</summary>
    void Clear();
}
