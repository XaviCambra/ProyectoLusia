using System;
using UnityEngine;

/// <summary>
/// Clase base serializable para todos los módulos de diálogo.
/// Gestiona el campo <c>runMode</c> que controla cómo el runner
/// gestiona la ejecución de este módulo.
/// </summary>
[Serializable]
public abstract class DialogueModuleBase : IDialogueModule
{
    [SerializeField]
    [Tooltip("FireAndForget: arranca y no espera.\nParallel: arranca junto al siguiente, se trackea.\nBlocking: espera todos los Parallel pendientes y luego espera este módulo.")]
    protected ModuleRunMode runMode = ModuleRunMode.Blocking;

    public ModuleRunMode RunMode
    {
        get => runMode;
        set => runMode = value;
    }

    public abstract string DisplayName { get; }
}
