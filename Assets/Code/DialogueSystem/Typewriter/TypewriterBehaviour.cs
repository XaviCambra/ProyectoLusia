using System.Threading;
using UnityEngine;
using TMPro;

public class TypewriterBehaviour : MonoBehaviour
{
    [SerializeField] private TMP_Text target;
    [SerializeField] private TypewriterProfile profile;
    [TextArea(3, 8)] public string demoText = "Hola, Guillem... ¿Preparado para escribir?";
    private CancellationTokenSource _cts;
    private TypewriterService _service = new();

    private async void Start()
    {
        _cts = new CancellationTokenSource();
        await _service.RunAsync(demoText, profile, target, null, _cts.Token);
    }

    private void OnDestroy() => _cts?.Cancel();
}
