using System.Linq;
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
    private ITypewriterPresenter typewriter;
    private IChoiceUIController choices;
    private IConditionEvaluator conditions;

    [Header("Controles")]
    [SerializeField] private KeyCode advanceKey = KeyCode.N;
    [SerializeField] private KeyCode restartKey = KeyCode.R;

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
        navigator = AsOrFind<IGraphNavigator>(navigatorBehaviour);
        portraits = AsOrFind<IPortraitController>(portraitsBehaviour);
        typewriter = AsOrFind<ITypewriterPresenter>(typewriterBehaviour);
        choices = AsOrFind<IChoiceUIController>(choicesBehaviour);
        conditions = AsOrFind<IConditionEvaluator>(conditionsBehaviour);

        // Validación mínima
        if (navigator == null) { Debug.LogError("[DialogueRunner] Falta IGraphNavigator."); enabled = false; return; }
        if (portraits == null) { Debug.LogError("[DialogueRunner] Falta IPortraitController."); enabled = false; return; }
        if (typewriter == null) { Debug.LogError("[DialogueRunner] Falta ITypewriterPresenter."); enabled = false; return; }
        if (choices == null) { Debug.LogError("[DialogueRunner] Falta IChoiceUIController."); enabled = false; return; }
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

        if (Input.GetKeyDown(restartKey))
        {
            Restart();
            return;
        }

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

        // 3) Retrato (sprite + colocación + animaciones especiales)
        await portraits.ApplyAsync(node);

        // 4) Texto (typewriter o directo)
        var resolvedText = navigator.ResolveBody(node);

        // Construimos un TypewriterProfile solo si el nodo lo requiere
        TypewriterProfile twProfile = null;
        if (node.useTypewriter)
        {
            twProfile = ScriptableObject.CreateInstance<TypewriterProfile>();
            twProfile.secondsPerChar = node.tw.secondsPerChar;
            twProfile.globalSpeed = node.tw.globalSpeed;
            twProfile.respectRichText = node.tw.respectRichText;
            twProfile.minimalWhitespaceDelay = node.tw.minimalWhitespaceDelay;
            twProfile.commaPct = node.tw.commaPct;
            twProfile.periodPct = node.tw.periodPct;
            twProfile.ellipsisPct = node.tw.ellipsisPct;
        }

        await typewriter.ShowAsync(resolvedText, twProfile);
        if (twProfile != null) Destroy(twProfile);

        // 5) Elecciones o listo para avanzar (delegación REAL a la UI)
        _waitingChoice = choices.Show(node, node.choices, OnChoiceSelected);
        // Si la UI decide no mostrar nada, quedamos listos para avanzar
        _nodeReadyToAdvance = !_waitingChoice;

        if (!_waitingChoice)
            _nodeReadyToAdvance = true; // no hay elecciones que mostrar → listo para avanzar
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

    private void Restart()
    {
        portraits.ResetAll();
        typewriter.Cancel();
        choices.Hide();

        _waitingChoice = false;
        _nodeReadyToAdvance = false;

        _current = navigator.StartNode();
        _ = ShowNodeAsync(_current);
    }

    private void EndDialogue()
    {
        portraits.ResetAll();
        typewriter.Cancel();
        choices.Hide();

        if (bodyText) bodyText.text = "<i>(Fin del diálogo)</i>";
        if (speakerText) speakerText.text = string.Empty;

        _current = null;
    }

    // Utilidad para castear o buscar componentes que implementen una interfaz
    private T AsOrFind<T>(MonoBehaviour mb) where T : class
    {
        if (mb is T ok) return ok;
        // Primero en este GameObject
        var local = GetComponents<MonoBehaviour>().OfType<T>().FirstOrDefault();
        if (local != null) return local;
        // Luego en toda la escena (incluyendo inactivos)
        var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                 .OfType<T>()
                 .FirstOrDefault();
        return all;
    }
}
