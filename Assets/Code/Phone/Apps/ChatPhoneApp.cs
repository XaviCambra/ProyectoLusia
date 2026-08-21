using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// App del movil que wrappea el sistema de chat. Orquesta 3 pantallas dentro
/// del panel de la app: lista de contactos (ChatRegistry, filtrados por
/// IsVisible) -> lista de conversaciones del contacto elegido (filtradas por
/// State != Hidden) -> chat activo (ChatUI reproduciendo esa conversacion).
/// </summary>
public class ChatPhoneApp : PhoneAppBase
{
    [Header("Motor")]
    [SerializeField] private DialogueRunner chatRunner;

    [Header("Pantallas")]
    [SerializeField] private GameObject contactsScreen;
    [SerializeField] private GameObject conversationsScreen;
    [SerializeField] private GameObject chatScreen;

    [Header("Listas")]
    [SerializeField] private ChatContactListView      contactListView;
    [SerializeField] private ChatConversationListView conversationListView;

    private ChatContact      _selectedContact;
    private ChatConversation _activeConversation;

    // Por que nodo (GUID) va cada conversacion, en memoria mientras dura la
    // sesion de juego. Cuando exista un sistema de guardado de partida, este
    // diccionario es lo que habria que cargar/guardar en disco; el resto de
    // esta clase no tendria que cambiar.
    private readonly Dictionary<ChatConversation, string> _resumeNodes = new();

    private void Awake()
    {
        if (contactListView != null)
            contactListView.OnContactSelected += ShowConversationsFor;

        if (conversationListView != null)
            conversationListView.OnConversationSelected += StartConversation;
    }

    private void OnDestroy()
    {
        if (contactListView != null)
            contactListView.OnContactSelected -= ShowConversationsFor;

        if (conversationListView != null)
            conversationListView.OnConversationSelected -= StartConversation;
    }

    public override void OnOpen() => ShowContacts();

    public override void OnClose()
    {
        SaveActiveConversationProgress();
        if (chatRunner) chatRunner.Stop();
    }

    // -----------------------------------------------------------------------
    // Navegacion entre pantallas. Enganchar tambien a los botones "atras".
    // -----------------------------------------------------------------------

    public void ShowContacts()
    {
        _selectedContact = null;
        SetScreen(contactsScreen);
        contactListView?.Refresh();
    }

    public void ShowConversationsFor(ChatContact contact)
    {
        _selectedContact = contact;
        SetScreen(conversationsScreen);
        conversationListView?.Show(contact);
    }

    /// <summary>Para el boton "atras" de la pantalla de chat: vuelve a las conversaciones del contacto actual.</summary>
    public void BackToConversations()
    {
        if (_selectedContact == null) { ShowContacts(); return; }
        ShowConversationsFor(_selectedContact);
    }

    private void StartConversation(ChatConversation conversation)
    {
        if (chatRunner == null || conversation == null || conversation.graph == null) return;

        SaveActiveConversationProgress();
        _activeConversation = conversation;
        SetScreen(chatScreen);

        _resumeNodes.TryGetValue(conversation, out var resumeNode);
        chatRunner.StartChat(conversation.graph, resumeNode);
    }

    /// <summary>Guarda por que nodo va la conversacion activa antes de dejarla (cambiar de chat o cerrar la app).</summary>
    private void SaveActiveConversationProgress()
    {
        if (_activeConversation == null || chatRunner == null) return;

        string node = chatRunner.CurrentNodeGuid;
        if (!string.IsNullOrEmpty(node))
            _resumeNodes[_activeConversation] = node;
    }

    private void SetScreen(GameObject screen)
    {
        if (contactsScreen)      contactsScreen.SetActive(screen == contactsScreen);
        if (conversationsScreen) conversationsScreen.SetActive(screen == conversationsScreen);
        if (chatScreen)          chatScreen.SetActive(screen == chatScreen);
    }
}
