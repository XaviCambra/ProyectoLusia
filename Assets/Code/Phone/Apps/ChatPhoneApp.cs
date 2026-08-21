using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// App del movil que wrappea el sistema de chat. Orquesta 3 pantallas dentro
/// del panel de la app: lista de contactos (ChatRegistry, filtrados por
/// IsVisible) -> lista de conversaciones del contacto elegido (filtradas por
/// State != Hidden) -> chat activo (ChatUI reproduciendo esa conversacion).
///
/// Cada conversacion activa tiene su propio DialogueRunner instanciado bajo
/// demanda, que sigue corriendo en segundo plano (timers, wait-for-signal,
/// mensajes) aunque no la tengas abierta ni la app de chat este abierta. Solo
/// se destruye cuando la conversacion llega a su nodo final. Ver
/// ConversationChatPresenter para como se conecta/desconecta la UI real.
/// </summary>
public class ChatPhoneApp : PhoneAppBase
{
    [Header("Motor")]
    [Tooltip("Prefab con un DialogueRunner y sus executors de chat (sin UI propia). Se instancia uno por conversacion activa.")]
    [SerializeField] private DialogueRunner runnerPrefab;
    [Tooltip("Donde se instancian los runners en segundo plano. Si se deja vacio, se usa este mismo transform.")]
    [SerializeField] private Transform runnerContainer;
    [Tooltip("Registro de contactos/conversaciones. Se usa para arrancar en segundo plano todas las conversaciones ya activas al iniciar, y las que se desbloqueen despues, sin esperar a que el jugador las abra.")]
    [SerializeField] private ChatRegistry registry;

    [Header("UI de chat")]
    [Tooltip("La UI real del chat (ChatUI), como IChatPresenter. Se conecta/desconecta segun la conversacion abierta.")]
    [SerializeField] private MonoBehaviour chatPresenterRef; // IChatPresenter

    [Header("Pantallas")]
    [SerializeField] private GameObject contactsScreen;
    [SerializeField] private GameObject conversationsScreen;
    [SerializeField] private GameObject chatScreen;

    [Header("Listas")]
    [SerializeField] private ChatContactListView      contactListView;
    [SerializeField] private ChatConversationListView conversationListView;

    private sealed class ChatThread
    {
        public DialogueRunner            runner;
        public ConversationChatPresenter presenter;
    }

    private readonly Dictionary<ChatConversation, ChatThread> _activeThreads = new();

    private IChatPresenter   _livePresenter;
    private ChatContact      _selectedContact;
    private ChatConversation _activeConversation;

    private void Awake()
    {
        _livePresenter = chatPresenterRef as IChatPresenter;

        if (contactListView != null)
            contactListView.OnContactSelected += ShowConversationsFor;

        if (conversationListView != null)
            conversationListView.OnConversationSelected += StartConversation;

        ForEachConversation(c => c.OnUnlocked += TryAutoStart);
        ForEachConversation(TryAutoStart);
    }

    private void OnDestroy()
    {
        if (contactListView != null)
            contactListView.OnContactSelected -= ShowConversationsFor;

        if (conversationListView != null)
            conversationListView.OnConversationSelected -= StartConversation;

        ForEachConversation(c => c.OnUnlocked -= TryAutoStart);
    }

    private void ForEachConversation(Action<ChatConversation> action)
    {
        if (registry == null) return;
        foreach (var contact in registry.contacts)
        {
            if (contact == null) continue;
            foreach (var conversation in contact.conversations)
                if (conversation != null)
                    action(conversation);
        }
    }

    /// <summary>Arranca el hilo de una conversacion en segundo plano (sin UI conectada) si esta
    /// activa y todavia no tiene uno. La usan tanto el arranque inicial como OnUnlocked.</summary>
    private void TryAutoStart(ChatConversation conversation)
    {
        if (conversation.State != ConversationState.Active) return;
        if (conversation.graph == null) return;
        if (_activeThreads.ContainsKey(conversation)) return;

        StartThread(conversation, live: null);
    }

    public override void OnOpen() => ShowContacts();

    /// <summary>
    /// Cerrar la app NO para las conversaciones en curso: siguen corriendo en
    /// segundo plano. Solo se desconecta la UI real de la que se estuviera viendo.
    /// </summary>
    public override void OnClose() => DetachActiveConversation();

    // -----------------------------------------------------------------------
    // Navegacion entre pantallas. Enganchar tambien a los botones "atras".
    // -----------------------------------------------------------------------

    public void ShowContacts()
    {
        DetachActiveConversation();
        _selectedContact = null;
        SetScreen(contactsScreen);
        contactListView?.Refresh();
    }

    public void ShowConversationsFor(ChatContact contact)
    {
        DetachActiveConversation();
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

    // -----------------------------------------------------------------------
    // Hilos de conversacion
    // -----------------------------------------------------------------------

    private void StartConversation(ChatConversation conversation)
    {
        if (conversation == null || conversation.graph == null) return;

        DetachActiveConversation();
        _activeConversation = conversation;
        SetScreen(chatScreen);
        ReplayHistory(conversation);

        if (_activeThreads.TryGetValue(conversation, out var thread))
        {
            thread.presenter.Attach(_livePresenter);
        }
        else if (conversation.State != ConversationState.Finished)
        {
            // La UI se conecta ANTES de arrancar: si no, el primer nodo corre entero
            // en silencio (el tipeo y los mensajes pasan por el presenter, que sin UI
            // conectada responde al instante) hasta el primer punto que de verdad
            // necesita jugador (un Choice), que es lo primero que se veria en pantalla.
            StartThread(conversation, _livePresenter);
        }

        conversation.MarkRead();
    }

    /// <summary>
    /// Repinta en la UI real el historial completo de la conversacion (mensajes de
    /// aperturas anteriores, incluidos los acumulados en segundo plano). Se hace
    /// antes de conectar la UI en directo para no duplicar mensajes ya mostrados.
    /// </summary>
    private void ReplayHistory(ChatConversation conversation)
    {
        _livePresenter?.Clear();
        foreach (var entry in conversation.History)
            _livePresenter?.AddMessage(entry);
    }

    /// <summary>Desconecta (sin parar) la conversacion que se estuviera mostrando en pantalla.</summary>
    private void DetachActiveConversation()
    {
        if (_activeConversation != null && _activeThreads.TryGetValue(_activeConversation, out var thread))
            thread.presenter.Attach(null);

        _activeConversation = null;
    }

    /// <summary>
    /// Instancia un runner nuevo para esta conversacion y la arranca desde el principio.
    /// El presenter se conecta a <paramref name="live"/> ANTES de StartChat para que el
    /// primer nodo ya se muestre en pantalla en vez de correr en silencio (ver StartConversation).
    /// </summary>
    private ChatThread StartThread(ChatConversation conversation, IChatPresenter live)
    {
        var runner    = Instantiate(runnerPrefab, runnerContainer ? runnerContainer : transform);
        var presenter = new ConversationChatPresenter(conversation);
        presenter.Attach(live);

        runner.SetPresenter(presenter);
        runner.OnDialogueEnded += () => EndThread(conversation);

        var thread = new ChatThread { runner = runner, presenter = presenter };
        _activeThreads[conversation] = thread;

        runner.StartChat(conversation.graph);
        return thread;
    }

    /// <summary>El grafo de una conversacion llego a su fin de forma natural: se marca terminada y se libera su runner.</summary>
    private void EndThread(ChatConversation conversation)
    {
        conversation.MarkFinished();

        if (!_activeThreads.TryGetValue(conversation, out var thread)) return;
        _activeThreads.Remove(conversation);
        if (thread.runner != null) Destroy(thread.runner.gameObject);
    }

    private void SetScreen(GameObject screen)
    {
        if (contactsScreen)      contactsScreen.SetActive(screen == contactsScreen);
        if (conversationsScreen) conversationsScreen.SetActive(screen == conversationsScreen);
        if (chatScreen)          chatScreen.SetActive(screen == chatScreen);
    }
}
