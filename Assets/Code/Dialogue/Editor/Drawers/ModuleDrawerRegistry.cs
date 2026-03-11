#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor;

/// <summary>
/// Registro centralizado de drawers para módulos de diálogo.
/// Cada tipo de módulo tiene una función de drawer que construye su UI en el editor.
/// Se inicializa automáticamente cuando Unity carga el editor.
/// </summary>
[InitializeOnLoad]
public static class ModuleDrawerRegistry
{
    // Firma del drawer: (módulo, callback onChange) → VisualElement con los campos
    private static readonly Dictionary<Type, Func<IDialogueModule, Action, VisualElement>> Drawers = new();

    static ModuleDrawerRegistry()
    {
        Register<TextModule>(TextModuleDrawer.Draw);
        Register<PortraitModule>(PortraitModuleDrawer.Draw);
        Register<EmoteModule>(EmoteModuleDrawer.Draw);
        Register<EventDispatcherModule>(EventModuleDrawer.Draw);
        Register<AudioModule>(AudioModuleDrawer.Draw);
        Register<ChoiceModule>(ChoiceModuleDrawer.Draw);
        Register<ChatPauseModule>(ChatPauseModuleDrawer.Draw);
        Register<ChatTypingModule>(ChatTypingModuleDrawer.Draw);
        Register<WaitForSignalModule>(WaitForSignalModuleDrawer.Draw);
    }

    /// <summary>
    /// Registra un drawer para el tipo de módulo T.
    /// </summary>
    public static void Register<T>(Func<T, Action, VisualElement> drawer) where T : IDialogueModule
        => Drawers[typeof(T)] = (m, cb) => drawer((T)m, cb);

    /// <summary>
    /// Construye y devuelve el VisualElement del drawer para el módulo dado.
    /// Si no hay drawer registrado, devuelve un label de aviso.
    /// </summary>
    public static VisualElement Draw(IDialogueModule module, Action onChanged)
    {
        if (module == null) return new Label("[Módulo nulo]");
        if (Drawers.TryGetValue(module.GetType(), out var fn))
            return fn(module, onChanged);
        return new Label($"[Sin drawer para '{module.GetType().Name}']");
    }

    /// <summary>
    /// Devuelve los tipos de módulo disponibles para el botón "Añadir módulo".
    /// Clave: nombre de display. Valor: tipo concreto.
    /// </summary>
    public static IReadOnlyDictionary<string, Type> AvailableModuleTypes => _available;

    private static readonly Dictionary<string, Type> _available = new()
    {
        ["Text"]              = typeof(TextModule),
        ["Portrait"]          = typeof(PortraitModule),
        ["Emote"]             = typeof(EmoteModule),
        ["Event"]             = typeof(EventDispatcherModule),
        ["Audio"]             = typeof(AudioModule),
        ["Choices"]           = typeof(ChoiceModule),
        ["Chat Pause"]        = typeof(ChatPauseModule),
        ["Chat Typing"]       = typeof(ChatTypingModule),
        ["Wait For Signal"]   = typeof(WaitForSignalModule),
    };
}
#endif
