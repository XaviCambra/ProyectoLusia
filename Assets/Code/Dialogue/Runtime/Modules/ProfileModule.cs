using System;

// USO: Solo Chat (ProfileModuleExecutor). Retratos no lo monta: ahi se salta. Ver IDialogueModule.
/// <summary>
/// Módulo de perfil: establece el <see cref="CharacterDefinition"/> activo para las
/// burbujas de chat siguientes. No bloquea ni produce output visual propio.
/// </summary>
[Serializable]
public class ProfileModule : DialogueModuleBase
{
    public override string DisplayName => "Profile";
    public override ModuleRunMode RunMode => ModuleRunMode.FireAndForget;

    public CharacterDefinition profile;
}
