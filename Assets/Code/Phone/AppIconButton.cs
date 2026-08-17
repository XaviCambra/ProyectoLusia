using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Botón de icono en el launcher del móvil. Generado automáticamente por <see cref="Phone"/>.
/// </summary>
public class AppIconButton : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private Image    iconImage;
    [SerializeField] private Button   button;

    public void Set(string appName, Sprite icon, UnityAction onClick)
    {
        if (nameLabel) nameLabel.text = appName;
        if (iconImage && icon) iconImage.sprite = icon;
        button.onClick.AddListener(onClick);
    }
}
