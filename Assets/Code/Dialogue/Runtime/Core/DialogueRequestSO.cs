using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Canal de solicitud de dialogo, basado en ScriptableObject (mismo patron que SignalSO).
/// Punto de entrada unico para pedir que se reproduzca un DialogueGraph en la escena de
/// Dialogo compartida, sin que quien lo pide necesite saber nada de escenas ni del
/// DialogueRunner. DialogueSceneEntryPoint es quien consume la solicitud al otro lado.
/// </summary>
[CreateAssetMenu(menuName = "Dialogue/Dialogue Request Channel", fileName = "DialogueRequestChannel")]
public sealed class DialogueRequestSO : ScriptableObject
{
    [Tooltip("Nombre de la escena de Dialogo compartida. Debe coincidir exactamente con su nombre en Build Settings.")]
    [SerializeField] private string dialogueSceneName = "DialogueScene";

    [NonSerialized] private DialogueGraph _pendingGraph;
    [NonSerialized] private string _pendingResumeNodeGuid;
    [NonSerialized] private bool _isActive;

    /// <summary>True mientras hay un dialogo pendiente de consumir o en curso.</summary>
    public bool IsActive => _isActive;

    /// <summary>Se dispara justo cuando una solicitud de dialogo se acepta, antes de cargar la escena.</summary>
    public event Action OnDialogueStarted;

    /// <summary>Se dispara cuando el dialogo en curso termina y la escena ya se ha descargado.</summary>
    public event Action OnDialogueFinished;

    /// <summary>
    /// Pide reproducir 'graph' en la escena de Dialogo compartida (se carga en additive).
    /// Devuelve false (con warning en consola, sin lanzar excepcion) si graph es null o si
    /// ya hay un dialogo activo — solo se admite una solicitud a la vez.
    /// </summary>
    public bool RequestDialogue(DialogueGraph graph, string resumeNodeGuid = null)
    {
        if (graph == null)
        {
            Debug.LogWarning("[DialogueRequestSO] graph nulo, solicitud ignorada.");
            return false;
        }

        if (_isActive)
        {
            Debug.LogWarning("[DialogueRequestSO] Ya hay un dialogo en curso, solicitud ignorada.");
            return false;
        }

        _pendingGraph = graph;
        _pendingResumeNodeGuid = resumeNodeGuid;
        _isActive = true;

        OnDialogueStarted?.Invoke();
        SceneManager.LoadSceneAsync(dialogueSceneName, LoadSceneMode.Additive);
        return true;
    }

    /// <summary>Consume la solicitud pendiente (se vacia tras leerla). Uso interno de DialogueSceneEntryPoint.</summary>
    internal bool TryConsumePending(out DialogueGraph graph, out string resumeNodeGuid)
    {
        graph = _pendingGraph;
        resumeNodeGuid = _pendingResumeNodeGuid;
        _pendingGraph = null;
        _pendingResumeNodeGuid = null;
        return graph != null;
    }

    /// <summary>Marca el dialogo como terminado y notifica. Uso interno de DialogueSceneEntryPoint.</summary>
    internal void NotifyFinished()
    {
        _isActive = false;
        OnDialogueFinished?.Invoke();
    }

    /// <summary>
    /// Red de seguridad manual: desbloquea IsActive si algo se quedo colgado (p.ej. la escena
    /// de Dialogo se cerro a mano sin pasar por el flujo normal). No forma parte del flujo normal.
    /// </summary>
    public void ForceReset()
    {
        _isActive              = false;
        _pendingGraph          = null;
        _pendingResumeNodeGuid = null;
    }
}
