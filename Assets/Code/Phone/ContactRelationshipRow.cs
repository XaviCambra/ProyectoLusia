using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fila de la app de Contactos. Muestra un contacto y su relacion con el
/// personaje activo en las dos direcciones (nunca entre otros personajes).
/// Instanciada por ContactsApp.
/// </summary>
public class ContactRelationshipRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private Image    avatarImage;

    [Header("Relacion activo -> contacto")]
    [SerializeField] private TMP_Text outgoingLabel;
    [SerializeField] private Image    outgoingColor;

    [Header("Relacion contacto -> activo")]
    [SerializeField] private TMP_Text incomingLabel;
    [SerializeField] private Image    incomingColor;

    public void Set(CharacterDefinition character, AffinityRelationship outgoing, AffinityRelationship incoming)
    {
        if (nameLabel) nameLabel.text = character.displayName;
        if (avatarImage && character.icon) avatarImage.sprite = character.icon;

        SetRelationship(outgoingLabel, outgoingColor, outgoing);
        SetRelationship(incomingLabel, incomingColor, incoming);
    }

    private static void SetRelationship(TMP_Text label, Image colorSwatch, AffinityRelationship relationship)
    {
        if (label) label.text = relationship?.DisplayName ?? "-";
        if (colorSwatch) colorSwatch.color = relationship?.color ?? Color.gray;
    }
}
