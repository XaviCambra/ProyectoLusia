using UnityEngine;

/// <summary>
/// Punto de entrada del sistema de afinidad. Coloca este MonoBehaviour en un
/// GameObject persistente (DontDestroyOnLoad). Crea el servicio, carga el
/// estado guardado y expone la instancia estática para todo el juego.
/// </summary>
public sealed class AffinityServiceBootstrapper : MonoBehaviour
{
    [SerializeField] private CharacterAffinityMap map;
    [SerializeField] private CharacterDatabase    characterDatabase;
    [SerializeField] private string               saveFileName = "affinity.json";

    public static IAffinityService Service { get; private set; }

    private AffinityMapService _service;

    private void Awake()
    {
        if (Service != null)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        var persistence = new JsonAffinityPersistence(saveFileName);
        _service = new AffinityMapService(map, characterDatabase, persistence);
        _service.Load();

        Service = _service;
    }

    private void OnApplicationQuit() => _service?.Save();
}
