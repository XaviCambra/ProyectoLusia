/// <summary>
/// Implementado por los executors de chat que renderizan via <see cref="IChatPresenter"/>.
/// Permite inyectar el presenter en tiempo de ejecucion (ej. al instanciar un
/// DialogueRunner dinamicamente para una conversacion en segundo plano), en vez
/// de depender solo de la referencia asignada en el Inspector.
/// </summary>
public interface IPresenterHost
{
    void SetPresenter(IChatPresenter presenter);
}
