using System;
using UnityEngine;

/// <summary>
/// Clase base serializable para todos los módulos de diálogo.
/// Gestiona el campo <c>blocks</c> que controla si el runner
/// espera a que este módulo complete antes de ejecutar el siguiente.
/// </summary>
[Serializable]
public abstract class DialogueModuleBase : IDialogueModule
{
    [SerializeField]
    [Tooltip("Si está activo, el runner espera a que este módulo termine antes de continuar con el siguiente.")]
    protected bool blocks = true;

    public bool Blocks
    {
        get => blocks;
        set => blocks = value;
    }

    public abstract string DisplayName { get; }
}
