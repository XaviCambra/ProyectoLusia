using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

/// <summary>
/// Executor para <see cref="TextModule"/>.
/// Muestra el nombre del hablante y el texto del diálogo (con typewriter opcional).
/// Soporta localización a través de un <see cref="ILocalizationService"/> opcional.
/// </summary>
[DisallowMultipleComponent]
public sealed class TextModuleExecutor : MonoBehaviour, IModuleExecutor
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI bodyText;

    [Header("Servicios")]
    [SerializeField] private MonoBehaviour typewriterRef;      // ITypewriterPresenter
    [SerializeField] private MonoBehaviour localizationRef;    // ILocalizationService (opcional)
    [SerializeField] private TypewriterProfile defaultProfile; // Perfil base de puntuación y opciones

    private ITypewriterPresenter _typewriter;
    private ILocalizationService _loc;

    public Type ModuleType => typeof(TextModule);

    private void Awake()
    {
        _typewriter = typewriterRef as ITypewriterPresenter;
        if (_typewriter == null)
            Debug.LogError("[TextModuleExecutor] Falta ITypewriterPresenter.");

        _loc = localizationRef as ILocalizationService;

        if (_typewriter != null && bodyText != null)
            _typewriter.Init(bodyText);
    }

    public void Initialize(DialogueGraph graph) { }

    public void OnNodeBegin()
    {
        if (speakerText) speakerText.text = string.Empty;
        _typewriter?.Cancel();
    }

    public async Task ExecuteAsync(IDialogueModule module, ModuleExecutionContext ctx)
    {
        var m = (TextModule)module;

        if (speakerText) speakerText.text = m.speakerName;

        var text = ResolveText(m);

        if (m.useTypewriter && _typewriter != null)
        {
            var profile = m.profileOverride ?? defaultProfile;
            using var reg = ctx.Token.Register(() => _typewriter?.Cancel());
            await _typewriter.ShowAsync(text, profile);
        }
        else
        {
            if (bodyText) bodyText.text = text;
        }
    }

    public void Cancel() => _typewriter?.Cancel();
    public bool TryFastForward() => _typewriter != null && _typewriter.FastForwardOrIgnore();

    public void ResetAll()
    {
        _typewriter?.Cancel();
        if (speakerText) speakerText.text = string.Empty;
        if (bodyText)    bodyText.text    = string.Empty;
    }

    private string ResolveText(TextModule m)
    {
        if (m.useLocalization && !string.IsNullOrEmpty(m.locKey) && _loc != null)
        {
            if (_loc.TryGet(m.locKey, out var localized))
                return localized;

            Debug.LogWarning($"[TextModuleExecutor] Clave de localización no encontrada: {m.locKey}");
        }
        return m.text ?? string.Empty;
    }
}
