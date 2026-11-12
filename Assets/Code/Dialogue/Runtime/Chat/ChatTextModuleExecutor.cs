using UnityEngine;

// USO: Solo sistema de Chat Bubble. El homologo de Retratos es TextModuleExecutor.
/// <summary>
/// Executor para <see cref="TextModule"/> en el motor de Chat: anade una burbuja de
/// texto. A diferencia de <see cref="TextModuleExecutor"/> (Retratos), no usa
/// typewriter ni localizacion: el chat muestra el mensaje completo de una vez.
/// Toda la logica esta en <see cref="ChatMessageExecutor{TModule}"/> y
/// <see cref="ChatEntryFactory"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChatTextModuleExecutor : ChatMessageExecutor<TextModule> { }
