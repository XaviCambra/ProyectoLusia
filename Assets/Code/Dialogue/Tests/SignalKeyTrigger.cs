using UnityEngine;

/// <summary>
/// Utilidad de test: pulsa una tecla configurable para lanzar un SignalSO.
/// Util para probar WaitForSignalModule sin depender de otro sistema del juego.
/// </summary>
public class SignalKeyTrigger : MonoBehaviour
{
    [SerializeField] private SignalSO signal;
    [SerializeField] private KeyCode  key = KeyCode.G;

    private void Update()
    {
        if (signal == null || !Input.GetKeyDown(key)) return;

        signal.Raise();
        Debug.Log($"[SignalKeyTrigger] Senal '{signal.name}' disparada (tecla {key}).");
    }
}
