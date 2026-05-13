using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Burbuja inline de opciones de texto. Se instancia en el Content del scroll
/// igual que un mensaje. Muestra botones de elección y resuelve la tarea cuando
/// el jugador elige; se destruye desde ChatUI en el bloque finally.
/// </summary>
public class ChoicesBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private Image    avatarImage;
    [SerializeField] private Transform buttonsParent;
    [SerializeField] private Button    buttonPrefab;

    private TaskCompletionSource<int> _tcs;

    public async Task<int> ShowAsync(IReadOnlyList<ChoiceModule.ChoiceData> choices, CharacterDefinition speaker, CancellationToken ct)
    {
        if (nameLabel)  nameLabel.text = speaker?.displayName ?? string.Empty;
        if (avatarImage && speaker?.avatarSprite != null)
            avatarImage.sprite = speaker.avatarSprite;

        _tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        for (int i = 0; i < choices.Count; i++)
        {
            var idx   = i;
            var btn   = Instantiate(buttonPrefab, buttonsParent);
            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label) label.text = choices[i].choiceText;
            btn.onClick.AddListener(() => Pick(idx));
        }

        using var reg = ct.Register(() => _tcs?.TrySetCanceled());
        return await _tcs.Task;
    }

    private void Pick(int idx)
    {
        _tcs?.TrySetResult(idx);
        _tcs = null;
    }
}
