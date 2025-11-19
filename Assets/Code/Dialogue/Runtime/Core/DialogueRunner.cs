using System.Threading.Tasks;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DialogueRunner : MonoBehaviour
{
    [Header("Graph")]
    [SerializeField] private DialogueGraph graph;

    [Header("Servicios (inyecta MonoBehaviours que implementen las interfaces)")]
    [SerializeField] private MonoBehaviour navigatorBehaviour;   // IGraphNavigator
    [SerializeField] private MonoBehaviour portraitsBehaviour;   // IPortraitController
    [SerializeField] private MonoBehaviour typewriterBehaviour;  // ITypewriterPresenter
    [SerializeField] private MonoBehaviour choicesBehaviour;     // IChoiceUIController
    [SerializeField] private MonoBehaviour conditionsBehaviour;  // IConditionEvaluator

    private IGraphNavigator navigator;
    private IPortraitController portraits;
    private IPortraitPlacementMilestones portraitsMilestones;
    private ITypewriterPresenter typewriter;
    private IChoiceUIController choices;
    private IConditionEvaluator conditions;

    [Header("Controles")]
    [SerializeField] private KeyCode advanceKey = KeyCode.N;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI bodyText;

    // Estado
    private DialogueNodeData _current;
    private bool _waitingChoice;
    private bool _nodeReadyToAdvance; // texto mostrado (y colocación hecha) en nodo NO-choices

    private void Awake()
    {
        if (graph == null)
        {
            Debug.LogError("[DialogueRunner] Falta DialogueGraph.");
            enabled = false; return;
        }

        // Casting de dependencias con fallback a búsqueda local/escena
        navigator = navigatorBehaviour as IGraphNavigator;
        portraits = portraitsBehaviour as IPortraitController;
        portraitsMilestones = portraitsBehaviour as IPortraitPlacementMilestones; // ← NUEVO
        typewriter = typewriterBehaviour as ITypewriterPresenter;
        choices = choicesBehaviour as IChoiceUIController;
        conditions = conditionsBehaviour as IConditionEvaluator;

        // Validación mínima
        if (navigator == null) { Debug.LogError("[DialogueRunner] Falta IGraphNavigator (o componente no implementa la interfaz)."); enabled = false; return; }
        if (portraits == null) { Debug.LogError("[DialogueRunner] Falta IPortraitController (o componente no implementa la interfaz)."); enabled = false; return; }
        if (typewriter == null) { Debug.LogError("[DialogueRunner] Falta ITypewriterPresenter (o componente no implementa la interfaz)."); enabled = false; return; }
        if (choices == null) { Debug.LogError("[DialogueRunner] Falta IChoiceUIController (o componente no implementa la interfaz)."); enabled = false; return; }
        if (conditions == null) { Debug.LogError("[DialogueRunner] Falta IConditionEvaluator."); enabled = false; return; }

        // Init de módulos
        navigator.Init(graph);
        portraits.Init(graph);
        typewriter.Init(bodyText);
        choices.Init();
    }

    private void Start()
    {
        _current = navigator.StartNode();
        _ = ShowNodeAsync(_current);
    }

    private void Update()
    {
        if (_current == null) return;

        // Hotkeys de elección (si estamos esperando elección)
        if (_waitingChoice && choices.TryConsumeHotkey(out var chosenPort))
        {
            OnChoiceSelected(chosenPort);
            return;
        }

        if (Input.GetKeyDown(advanceKey))
        {
            // 1) Si hay typewriter activo, lo completamos y NO avanzamos aún
            if (typewriter.FastForwardOrIgnore()) return;

            // 2) Si no hay elecciones y el nodo ya está listo para avanzar, pasamos al siguiente
            if (!_waitingChoice && _nodeReadyToAdvance)
            {
                AdvanceToNext();
                return;
            }
        }
    }

    private async Task ShowNodeAsync(DialogueNodeData node)
    {
        _waitingChoice = false;
        _nodeReadyToAdvance = false;

        choices.BeginNode();

        if (node == null) { EndDialogue(); return; }

        // 1) Eventos de entrada
        navigator.RaiseEnterEvents(node);

        // 2) Speaker
        if (speakerText) speakerText.text = navigator.ResolveSpeaker(node);

        // 3) Texto + retrato con timings finos
        var resolvedText = navigator.ResolveBody(node);

        // Construimos un TypewriterProfile solo si el nodo lo requiere
        TypewriterProfile twProfile = null;
        if (node.useTypewriter)
        {
            twProfile = ScriptableObject.CreateInstance<TypewriterProfile>();
            twProfile.secondsPerChar = node.tw.secondsPerChar;
            twProfile.globalSpeed = node.tw.globalSpeed;
        }

        // 4) Lanzar retrato con hitos y disparar texto según TextStartTiming
        if (portraitsMilestones != null)
        {
            // Nuevo flujo con hitos de colocación
            var ms = portraitsMilestones.ApplyWithMilestones(node);

            switch (node.textStart)
            {
                case TextStartTiming.OnEnterStart:
                    portraits.OnTextStart(node);                         // dispara especiales "WithTextStart"
                    await typewriter.ShowAsync(resolvedText, twProfile); // empieza YA el texto
                    await ms.Complete;                                   // asegura que la colocación acabe antes de elecciones
                    break;

                case TextStartTiming.OnEnterMid:
                    await ms.Mid;                                        // espera a ~50% de la colocación
                    portraits.OnTextStart(node);
                    await typewriter.ShowAsync(resolvedText, twProfile);
                    await ms.Complete;                                   // garantiza fin de colocación antes de elecciones
                    break;

                case TextStartTiming.OnEnterComplete:
                default:
                    await ms.Complete;                                   // comportamiento clásico
                    portraits.OnTextStart(node);
                    await typewriter.ShowAsync(resolvedText, twProfile);
                    break;
            }
        }
        else
        {
            // Fallback retro-compatible: sin hitos → como antes (espera a terminar)
            await portraits.ApplyAsync(node);
            portraits.OnTextStart(node);
            await typewriter.ShowAsync(resolvedText, twProfile);
        }

        // Limpieza del perfil temporal
        if (twProfile != null) Destroy(twProfile);

        // 5) Elecciones o listo para avanzar (delegación REAL a la UI)
        _waitingChoice = choices.Show(node, node.choices, OnChoiceSelected);
        // Si la UI decide no mostrar nada, quedamos listos para avanzar
        _nodeReadyToAdvance = !_waitingChoice;
    }

    private void OnChoiceSelected(string fromPort)
    {
        _waitingChoice = false;
        choices.Hide();

        var next = navigator.NextFrom(_current, fromPort);
        if (next != null)
        {
            _current = next;
            _ = ShowNodeAsync(_current);
        }
        else
        {
            EndDialogue();
        }
    }

    private void AdvanceToNext()
    {
        var next = navigator.NextFrom(_current);
        if (next != null)
        {
            _current = next;
            _ = ShowNodeAsync(_current);
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        ResetUiAndState();
        if (bodyText) bodyText.text = "<i>(Fin del diálogo)</i>";
        if (speakerText) speakerText.text = string.Empty;
        _current = null;
    }

    private void ResetUiAndState()
    {
        // Apagar visuales
        portraits.ResetAll();
        typewriter.Cancel();
        choices.Hide();

        // Resetear estado interno
        _waitingChoice = false;
        _nodeReadyToAdvance = false;
    }
}
