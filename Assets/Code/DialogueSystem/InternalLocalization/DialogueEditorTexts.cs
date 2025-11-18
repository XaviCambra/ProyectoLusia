#if UNITY_EDITOR
/// <summary>
/// Textos de la interfaz del editor de nodos de di�logo.
/// Solo se usa en Editor, no se compila en el juego.
/// </summary>
public static class DialogueEditorTexts
{
    // -----------------------------
    // Foldouts principales (AddDivider) OK
    // -----------------------------
    public const string FoldoutProfileAndBackground = "Perfil y color de fondo";
    public const string FoldoutTextAndLocalization = "Texto y localización";
    public const string FoldoutAppearanceAndAnimation = "Apariencia y animación";
    public const string FoldoutSpecialAnimation = "Animación especial";
    public const string FoldoutTypewriter = "Typewriter";
    public const string FoldoutDialogueFlow = "Flujo del diólogo";
    public const string FoldoutEvents = "Eventos";

    // -----------------------------
    // Título OK
    // -----------------------------
    public const string NodeTitleName = "Diálogo";

    // -----------------------------
    // Cabecera / botones OK
    // -----------------------------
    public const string ButtonDeleteNode = "Eliminar nodo";

    // -----------------------------
    // Perfil / color de fondo OK
    // -----------------------------
    public const string LabelBackgroundColor = "Color de fondo";
    public const string LabelProfileAsset = "Perfil (SO)";
    public const string LabelPortraitKey = "Retrato (clave)";

    public const string TooltipProfileAsset =
        "Perfil de personaje que habla en este nodo.";

    public const string TooltipPortraitKey =
        "Clave del retrato dentro del perfil seleccionado que se mostrar� en pantalla.";

    // -----------------------------
    // Texto y localización del nodo OK
    // -----------------------------
    public const string LabelSpeakerName = "Nombre";
    public const string LabelLocalizationToggle = "Localización";
    public const string LabelLiteralText = "Texto";
    public const string LabelNodeLocalizationKey = "Clave de localización";

    public const string TooltipLocalizationToggleNode =
        "Activa para usar una clave de localización en vez de texto literal en este nodo.";

    // -----------------------------
    // Apariencia y animación (entrada) OK
    // -----------------------------
    public const string LabelEnterMode = "Aparición";
    public const string LabelEnterOrigin = "Origen";
    public const string LabelEnterTarget = "Destino";
    public const string LabelEnterMoveSpeed = "Velocidad movimiento (px/s)";
    public const string LabelTextStartPosition = "Inicio del texto";

    public const string LabelUseFade = "Usar desvanecido";
    public const string LabelEnterFromOpacity = "Opacidad inicial (%)";
    public const string LabelEnterToOpacity = "Opacidad final (%)";

    public const string TooltipEnterMode =
        "Define cómo entra el retrato en pantalla (estatico, deslizondose).";

    // -----------------------------
    // Animación especial OK
    // -----------------------------
    public const string LabelSpecialAnimToggle = "Usar animación especial";
    public const string LabelSpecialAnimClip = "Clip de animación";
    public const string LabelSpecialAnimSpeed = "Velocidad";
    public const string LabelSpecialAnimLoop = "Repetir (loop)";
    public const string LabelSpecialAnimStart = "Inicio anim. especial";

    public const string TooltipSpecialAnim =
        "Permite reproducir un clip de animación sobre el retrato mientras dura el nodo.";

    // -----------------------------
    // Typewriter OK
    // -----------------------------
    public const string LabelTypewriterToggle = "Typewriter";
    public const string LabelSecondsPerChar = "Segundos por caracter";
    public const string LabelGlobalTypewriterSpeed = "Velocidad global (x)";

    public const string TooltipTypewriterToggle =
        "Activa la escritura progresiva del texto en este nodo.";

    public const string TooltipSecondsPerChar =
        "Tiempo base que tarda en aparecer cada car�cter.";

    public const string TooltipGlobalTypewriterSpeed =
        "Factor multiplicador de la velocidad total del efecto typewriter.";

    // -----------------------------
    // Flujo del diologo OK
    // -----------------------------
    public const string LabelIsStartNode = "Es nodo inicio";
    public const string LabelStartId = "Identificador de inicio";
    public const string LabelIsChoiceNode = "Es nodo de elección";
    public const string LabelChoiceCount = "Número de opciones";
    public const string LabelShowBlockedChoices = "Mostrar opciones";

    public const string TooltipIsStartNode =
        "Si esta activo, este nodo puede usarse como punto de entrada del di�logo.";

    public const string TooltipIsChoiceNode =
        "Si esta activo, este nodo genera varias opciones de salida.";

    public const string TooltipShowBlockedChoices =
        "Si esta activo, las opciones que no cumplan requisitos se ver�n deshabilitadas en lugar de ocultas.";

    // -----------------------------
    // Opciones: texto y localizaci�n OK
    // -----------------------------
    public const string LabelChoiceBase = "Opci�n";
    public const string LabelChoiceLocalizationKey = "Clave localizaci�n";
    public const string LabelChoiceLocalizationToggle = "Localizaci�n";

    public const string TooltipChoiceLocalizationToggle =
        "Activa para que el texto de esta opci�n venga de una clave de localizaci�n.";

    // -----------------------------
    // Requisitos generales OK
    // -----------------------------
    public const string LabelRequirements = "Requisitos";
    
    // -----------------------------
    // Requisitos: Afinidad OK
    // -----------------------------
    public const string LabelAffinityRequirementToggle = "Req. afinidad";
    public const string LabelAffinityKey = "Clave";
    public const string LabelAffinityValue = "Valor";
    public const string LabelAffinityInvert = "Baja afinidad";

    public const string TooltipAffinityRequirementToggle =
        "Si est� activo, esta opci�n solo se habilita si la afinidad cumple el valor indicado.";

    public const string TooltipAffinityInvert =
        "Si est� activo, la condici�n se invierte (por ejemplo, afinidad por debajo de un valor).";

    // -----------------------------
    // Requisitos: Progreso OK
    // -----------------------------
    public const string LabelProgressRequirementToggle = "Req. progreso";
    public const string LabelProgressMethod = "M�todo";
    public const string LabelProgressArgType = "Tipo de argumento";
    public const string LabelProgressIntValue = "Valor (entero)";
    public const string LabelProgressFloatValue = "Valor (decimal)";
    public const string LabelProgressStringValue = "Valor (texto)";

    public const string TooltipProgressRequirementToggle =
        "Si est� activo, esta opci�n depende de una condici�n de progreso definida por c�digo.";

    public const string TooltipProgressMethod =
        "Nombre del m�todo o clave que usar� el sistema de progreso para evaluar la condici�n.";

    // -----------------------------
    // Eventos
    // -----------------------------
    public const string LabelEventKey = "Clave de evento";
    public const string LabelEventPayloadType = "Tipo de payload";
    public const string LabelEventButtonTest = "Probar evento";

    public const string LabelEventIntValue = "Valor (entero)";
    public const string LabelEventFloatValue = "Valor (float)";
    public const string LabelEventStringValue = "Valor (texto)";
    public const string LabelEventBoolValue = "Valor (bool)";
    public const string LabelEventCharValue = "Valor (car�cter)";

    public const string TooltipEventKey =
        "Identificador del evento que se disparar� al llegar a este nodo o elegir esta opci�n.";

    public const string TooltipEventPayloadType =
        "Tipo de dato adicional que se enviar� junto al evento.";

    public const string TooltipEventButtonTest =
        "Dispara el evento ahora mismo desde el editor para probar integraciones.";
}
#endif
