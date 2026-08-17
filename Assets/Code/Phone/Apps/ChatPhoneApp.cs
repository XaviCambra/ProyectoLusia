using UnityEngine;

/// <summary>
/// App del móvil que wrappea el sistema de chat.
/// Asigna <see cref="chatRunner"/> (un <see cref="DialogueRunner"/> configurado
/// con los executors de chat) y opcionalmente un <see cref="defaultGraph"/>
/// para arrancar automáticamente al abrir la app.
/// </summary>
public class ChatPhoneApp : PhoneAppBase
{
    [SerializeField] private DialogueRunner chatRunner;
    [SerializeField] private DialogueGraph  defaultGraph;

    public override void OnOpen()
    {
        if (chatRunner && defaultGraph)
            chatRunner.StartChat(defaultGraph);
    }

    public override void OnClose()
    {
        if (chatRunner) chatRunner.Stop();
    }
}
