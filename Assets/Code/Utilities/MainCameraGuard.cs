using UnityEngine;

/// <summary>
/// Evita tener dos camaras "MainCamera" activas a la vez cuando esta escena se carga additive
/// encima de otra que ya trae la suya (ej. Dialogues sobre Bunker) -- mismo patron que
/// EventSystemGuard, aplicado a Camera. Si al activarse ya hay otra camara con tag MainCamera
/// activa, esta se autodestruye entera y se queda la que ya estaba (normalmente la que sigue
/// al jugador).
///
/// Si esta escena se abre sola, no hay ninguna otra MainCamera todavia y esta se queda tal
/// cual -- por eso cada escena puede seguir teniendo la suya para poder probarse suelta.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class MainCameraGuard : MonoBehaviour
{
    private const string MainCameraTag = "MainCamera";

    private void Awake()
    {
        if (!CompareTag(MainCameraTag)) return;

        foreach (var cam in Camera.allCameras)
        {
            if (cam.gameObject != gameObject && cam.CompareTag(MainCameraTag))
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}
