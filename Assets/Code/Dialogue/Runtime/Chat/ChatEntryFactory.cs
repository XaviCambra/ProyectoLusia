/// <summary>
/// Convierte un modulo de contenido de chat (Text / Emoji / Image) en la
/// <see cref="ChatEntry"/> que se pinta como burbuja. Funcion pura: no depende de
/// MonoBehaviours ni del presenter, asi que la usan tanto los executors en vivo
/// (<see cref="ChatMessageExecutor{TModule}"/>) como cualquier codigo que necesite
/// reconstruir mensajes sin ejecutar el grafo (p.ej. rehacer el historial al cargar
/// una partida).
///
/// Regla del hablante (igual para los tres tipos): el perfil activo
/// (<see cref="ProfileModule"/>) pone nombre y avatar. En <see cref="TextModule"/>,
/// <c>speakerName</c> es opcional y, si no esta vacio, sustituye al nombre del perfil.
/// </summary>
public static class ChatEntryFactory
{
    /// <summary>
    /// Devuelve true y la entrada si el modulo genera un mensaje de chat; false si no es
    /// un modulo de contenido o no tiene contenido (emoji/imagen sin sprite).
    /// </summary>
    public static bool TryCreate(IDialogueModule module, CharacterDefinition profile, out ChatEntry entry)
    {
        var profileName = profile != null ? profile.displayName : null;
        var avatar      = profile != null ? profile.avatarSprite : null;

        switch (module)
        {
            case TextModule t:
                var speaker = string.IsNullOrEmpty(t.speakerName) ? profileName : t.speakerName;
                entry = ChatEntry.ForText(speaker, t.text, t.isOwn, avatar);
                return true;

            case EmojiModule e when e.emoji != null:
                entry = ChatEntry.ForEmoji(profileName, e.emoji, e.isOwn, avatar);
                return true;

            case ImageModule i when i.image != null:
                entry = ChatEntry.ForImage(profileName, i.image, i.isOwn, avatar);
                return true;

            default:
                entry = default;
                return false;
        }
    }
}
