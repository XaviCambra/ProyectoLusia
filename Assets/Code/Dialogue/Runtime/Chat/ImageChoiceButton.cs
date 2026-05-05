using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Componente raíz del prefab de botón de opción de imagen.
/// Encapsula su propia estructura interna; ChatUI solo llama a Set().
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class ImageChoiceButton : MonoBehaviour
{
    [SerializeField] private Image contentImage;

    private Button _button;

    private void Awake() => _button = GetComponent<Button>();

    public void Set(Sprite sprite, UnityAction onClick)
    {
        if (contentImage) contentImage.sprite = sprite;
        if (_button == null) _button = GetComponent<Button>();
        _button.onClick.AddListener(onClick);
    }
}
