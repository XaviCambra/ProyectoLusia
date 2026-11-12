using UnityEngine;

// USO: Solo sistema de Chat Bubble. Ignorado por Retratos (no hay executor equivalente).
/// <summary>
/// Executor para <see cref="EmojiModule"/>: anade una burbuja de emoji.
/// Toda la logica esta en <see cref="ChatMessageExecutor{TModule}"/> y
/// <see cref="ChatEntryFactory"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class EmojiModuleExecutor : ChatMessageExecutor<EmojiModule> { }
