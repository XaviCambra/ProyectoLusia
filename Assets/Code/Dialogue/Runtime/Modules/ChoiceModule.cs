using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Módulo de elección: presenta al jugador un conjunto de opciones
/// y espera a que seleccione una. Siempre es bloqueante.
/// Es el único módulo que genera puertos de salida dinámicos en el
/// editor (uno por opción), en lugar del puerto "Next" estándar.
/// </summary>
[Serializable]
public class ChoiceModule : DialogueModuleBase
{
    public override string DisplayName => "Choices";

    // Las choices SIEMPRE son Blocking — no tiene sentido continuar sin una selección
    public new ModuleRunMode RunMode
    {
        get => ModuleRunMode.Blocking;
        set { } // No-op: ignoramos asignaciones externas
    }

    [Tooltip("Si está activo, las opciones bloqueadas se muestran deshabilitadas. Si no, se ocultan.")]
    public bool showBlockedChoices = false;

    [Tooltip("Lista de opciones disponibles en este nodo.")]
    public List<ChoiceData> choices = new();

    // -----------------------------------------------------------------------

    /// <summary>
    /// Datos de una opción individual dentro del ChoiceModule.
    /// </summary>
    [Serializable]
    public class ChoiceData
    {
        [Tooltip("Texto visible de la opción (literal).")]
        public string choiceText = "Opción";

        [Tooltip("Nombre del puerto de salida. Debe ser único dentro del nodo.")]
        public string portName = "";

        // --- Localización ---
        [Tooltip("Si está activo, se usa la clave de localización en lugar del texto literal.")]
        public bool choiceUseLocalization = false;

        [Tooltip("Clave de localización para el texto de la opción.")]
        public string choiceLocKey = "";

        // --- Condición: Afinidad ---
        [Tooltip("Si está activo, esta opción requiere un nivel mínimo de afinidad.")]
        public bool requiresAffinity = false;

        [Tooltip("Clave del parámetro de afinidad a evaluar.")]
        public string affinityKey = "";

        [Tooltip("Valor mínimo requerido de afinidad.")]
        public float requiredAffinity = 0f;

        [Tooltip("Si está activo, invierte el requisito (la afinidad debe ser MENOR que el umbral).")]
        public bool invertRequirement = false;

        // --- Condición: Progreso ---
        [Tooltip("Si está activo, esta opción requiere que una condición de progreso sea verdadera.")]
        public bool requiresProgress = false;

        [Tooltip("Nombre del método evaluador registrado en ProgressConditionInvoker.")]
        public string progressMethod = "";

        [Tooltip("Tipo del argumento que se pasa al método evaluador.")]
        public ProgressArgType progressArgType = ProgressArgType.None;

        [Tooltip("Argumento entero para el método evaluador.")]
        public int progressArgInt = 0;

        [Tooltip("Argumento float para el método evaluador.")]
        public float progressArgFloat = 0f;

        [Tooltip("Argumento string para el método evaluador.")]
        public string progressArgString = "";
    }
}
