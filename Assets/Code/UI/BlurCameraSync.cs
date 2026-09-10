using UnityEngine;

/// <summary>
/// Copia la transform y los parametros de proyeccion de 'sourceCamera' a la camara de este
/// mismo GameObject, cada frame. Se usa en la camara secundaria que renderiza a la
/// RenderTexture del panel de blur, para que muestre exactamente lo mismo que ve la camara
/// principal.
/// </summary>
[RequireComponent(typeof(Camera))]
public class BlurCameraSync : MonoBehaviour
{
    [SerializeField] private Camera sourceCamera;

    private Camera _camera;

    private void Awake() => _camera = GetComponent<Camera>();

    private void LateUpdate()
    {
        if (sourceCamera == null) return;

        transform.SetPositionAndRotation(sourceCamera.transform.position, sourceCamera.transform.rotation);

        _camera.orthographic     = sourceCamera.orthographic;
        _camera.orthographicSize = sourceCamera.orthographicSize;
        _camera.fieldOfView      = sourceCamera.fieldOfView;
        _camera.nearClipPlane    = sourceCamera.nearClipPlane;
        _camera.farClipPlane     = sourceCamera.farClipPlane;
    }
}
