using UnityEngine;

/// <summary>
/// App de contactos: lista, para el personaje activo, a todos los personajes
/// con relacion de afinidad CONOCIDA (IAffinityService.IsKnown) y muestra la
/// relacion en ambas direcciones (nunca entre otros personajes distintos del
/// activo). Toda la logica de "quien cuenta como conocido" vive en
/// IAffinityService.GetKnownCounterparts — esta clase solo pide esa lista y
/// la pinta, nada mas. Asi el dato y la UI quedan separados: si el dia de
/// manana cambia el criterio de que es "conocido", se cambia en un solo sitio
/// (el servicio) y esta app ni se entera.
///
/// El personaje activo es un campo fijo por Inspector por ahora (no existe
/// todavia un concepto de "personaje al que se le paso el movil" en el resto
/// del juego); cuando exista, este campo pasa a fijarse desde fuera en vez de
/// ser fijo en el prefab.
/// </summary>
public class ContactsApp : PhoneAppBase
{
    [Tooltip("Personaje cuya perspectiva se muestra. Fijo por Inspector por ahora.")]
    [SerializeField] private CharacterDefinition activeCharacter;

    [SerializeField] private Transform contentParent;
    [SerializeField] private ContactRelationshipRow rowPrefab;

    public override void OnOpen() => Refresh();

    private void Refresh()
    {
        contentParent?.DestroyAllChildren();
        if (activeCharacter == null || contentParent == null || rowPrefab == null) return;

        var affinity = AffinityServiceBootstrapper.Service;
        if (affinity == null) return;

        foreach (var contact in affinity.GetKnownCounterparts(activeCharacter))
        {
            var row = Instantiate(rowPrefab, contentParent);
            row.Set(contact.Character, contact.Outgoing, contact.Incoming);
        }
    }
}
