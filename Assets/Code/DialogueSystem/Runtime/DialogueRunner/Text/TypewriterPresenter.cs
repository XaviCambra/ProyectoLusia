using System;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TypewriterPresenter : MonoBehaviour, ITypewriterPresenter
{
    [SerializeField] private TextMeshProUGUI target;

    private readonly TypewriterService _service = new();
    private CancellationTokenSource _cts;
    private string _lastFullText = string.Empty;
    private bool _isTyping;

    public void Init(TextMeshProUGUI t)
    {
        target = t;
    }

    public async Task ShowAsync(string text, TypewriterProfile profileOrNull)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        _lastFullText = text ?? string.Empty;
        if (!target)
        {
            _isTyping = false;
            return;
        }

        if (profileOrNull == null)
        {
            target.SetText(_lastFullText);
            _isTyping = false;
            return;
        }

        _isTyping = true;
        try
        {
            // Firma esperada según tu código anterior: RunAsync(string, TypewriterProfile, TMP, object, CancellationToken)
            await _service.RunAsync(_lastFullText, profileOrNull, target, null, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Fast-forward o cambio de nodo
        }
        finally
        {
            _isTyping = false;
        }
    }

    public bool FastForwardOrIgnore()
    {
        if (!_isTyping) return false;

        _cts?.Cancel();
        if (target) target.SetText(_lastFullText);
        _isTyping = false;
        return true;
    }

    public void Cancel()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _isTyping = false;
    }
}
