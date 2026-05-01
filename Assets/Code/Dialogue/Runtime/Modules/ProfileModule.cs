using System;

// USO: Solo sistema de Chat Bubble. Ignorado por DialogueRunner.
/// <summary>
/// Módulo de perfil: establece el <see cref="CharacterProfile"/> activo para las
/// burbujas de chat siguientes. No bloquea ni produce output visual propio.
/// </summary>
[Serializable]
public class ProfileModule : DialogueModuleBase
{
    public override string DisplayName => "Profile";
    public override ModuleRunMode RunMode => ModuleRunMode.FireAndForget;

    public CharacterProfile profile;
}
