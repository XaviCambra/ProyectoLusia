using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using TMPro;

public class TypewriterSequenceTest : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TMP_Text target;
    [SerializeField] private TypewriterProfile profile;

    [Header("Líneas de diálogo")]
    [TextArea(2, 4)]
    [SerializeField]
    private List<string> lines = new()
    {
        "1.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "2.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "3.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "4.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "5.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "6.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "7.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "8.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "9.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "10.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "11.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "12.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "13.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "14.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "15.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "16.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "17.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "18.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book.",
        "19.- Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book."
    };

    private int _currentIndex = 0;
    private CancellationTokenSource _cts;
    private TypewriterService _service = new();
    private bool _isWriting = false;

    private void Start()
    {
        if (target == null || profile == null)
        {
            Debug.LogWarning("[TypewriterSequenceTest] Asigna un TMP_Text y un TypewriterProfile.");
            enabled = false;
            return;
        }

        // Inicia la primera línea automáticamente
        _cts = new CancellationTokenSource();
        _ = WriteCurrentLineAsync();
    }

    private async System.Threading.Tasks.Task WriteCurrentLineAsync()
    {
        if (_currentIndex < 0 || _currentIndex >= lines.Count)
        {
            Debug.Log("✅ Fin del diálogo.");
            return;
        }

        _isWriting = true;
        target.text = "";

        await _service.RunAsync(
            text: lines[_currentIndex],
            profile: profile,
            target: target,
            ct: _cts.Token
        );

        _isWriting = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (_isWriting)
            {
                // Si el texto aún se está escribiendo → saltar
                SkipCurrentLineInstant();
            }
            else
            {
                // Avanzar a la siguiente línea
                NextLine();
            }
        }
    }

    private void SkipCurrentLineInstant()
    {
        _cts.Cancel(); // cancela el tipeo actual
        target.text = lines[_currentIndex]; // muestra todo el texto al instante
        _isWriting = false;
    }

    private void NextLine()
    {
        _currentIndex++;
        if (_currentIndex >= lines.Count)
        {
            Debug.Log("✅ Fin de todas las líneas.");
            target.text = "<color=#999>FIN DEL TEST</color>";
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _ = WriteCurrentLineAsync();
    }

    private void OnDestroy() => _cts?.Cancel();
}
