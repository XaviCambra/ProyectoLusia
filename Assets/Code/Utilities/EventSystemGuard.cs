using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Evita tener dos EventSystem activos a la vez cuando esta escena se carga additive encima de
/// otra que ya trae el suyo (ej. DialogueScene sobre Bunker). Si al activarse ya hay un
/// EventSystem distinto activo, este se autodestruye entero -- mismo patron que
/// AffinityServiceBootstrapper para no duplicar su servicio, aplicado aqui al EventSystem.
///
/// Si esta escena se abre sola (Play directo sobre ella, o es la primera escena cargada), no
/// hay ningun otro EventSystem todavia y este se queda tal cual, funcionando con normalidad --
/// por eso cada escena puede seguir teniendo el suyo para poder probarse suelta.
/// </summary>
[RequireComponent(typeof(EventSystem))]
public sealed class EventSystemGuard : MonoBehaviour
{
    private void Awake()
    {
        var mine = GetComponent<EventSystem>();
        if (EventSystem.current != null && EventSystem.current != mine)
            Destroy(gameObject);
    }
}
