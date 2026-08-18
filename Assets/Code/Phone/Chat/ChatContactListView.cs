using System;
using UnityEngine;

/// <summary>
/// Puebla la lista de contactos visibles (ChatContact.IsVisible) de un ChatRegistry.
/// No decide navegacion: solo notifica que contacto se ha elegido.
/// </summary>
public class ChatContactListView : MonoBehaviour
{
    [SerializeField] private ChatRegistry   registry;
    [SerializeField] private Transform      contentParent;
    [SerializeField] private ChatContactRow rowPrefab;

    public event Action<ChatContact> OnContactSelected;

    public void Refresh()
    {
        Clear();
        if (registry == null || contentParent == null || rowPrefab == null) return;

        foreach (var contact in registry.contacts)
        {
            if (contact == null || !contact.IsVisible) continue;

            var row = Instantiate(rowPrefab, contentParent);
            row.Set(contact, () => OnContactSelected?.Invoke(contact));
        }
    }

    public void Clear() => contentParent?.DestroyAllChildren();
}
