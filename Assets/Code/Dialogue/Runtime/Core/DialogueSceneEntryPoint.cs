using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Puente entre DialogueRequestSO y el DialogueRunner de la escena de Dialogo compartida.
/// Al arrancar, consume la solicitud pendiente (si hay) y arranca el dialogo; al terminar,
/// notifica al canal y descarga la escena. Es el unico componente de esta escena que conoce
/// tanto al canal de solicitud como al runner.
/// </summary>
[DisallowMultipleComponent]
public sealed class DialogueSceneEntryPoint : MonoBehaviour
{
    [SerializeField] private DialogueRequestSO request;
    [SerializeField] private DialogueRunner    runner;

    private void Start()
    {
        if (request == null || runner == null)
        {
            Debug.LogError("[DialogueSceneEntryPoint] Falta 'request' o 'runner' en el Inspector.");
            return;
        }

        if (!request.TryConsumePending(out var graph, out var resumeNodeGuid))
        {
            // Sin solicitud pendiente: probablemente se le ha dado a Play directamente sobre
            // esta escena para probarla a mano. No se hace nada mas — si el DialogueRunner
            // tiene un graph propio asignado en el Inspector, su Start() normal lo reproduce
            // igual que siempre (comportamiento sin cambios para pruebas locales).
            return;
        }

        runner.OnDialogueEnded += HandleDialogueEnded;
        runner.StartChat(graph, resumeNodeGuid);
    }

    private void HandleDialogueEnded()
    {
        runner.OnDialogueEnded -= HandleDialogueEnded;
        request.NotifyFinished();

        if (SceneManager.sceneCount > 1)
            SceneManager.UnloadSceneAsync(gameObject.scene);
        else
            Debug.LogWarning("[DialogueSceneEntryPoint] Es la unica escena cargada, no se descarga.");
    }

    private void OnDestroy()
    {
        if (runner != null)
            runner.OnDialogueEnded -= HandleDialogueEnded;
    }
}
