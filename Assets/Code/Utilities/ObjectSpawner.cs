using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    void SpawnObject(GameObject _Object)
    {
        Instantiate(_Object, Vector3.zero, new Quaternion(0, 0, 0, 1));
    }

    void SpawnObject(GameObject _Object, Transform _Parent)
    {
        Instantiate(_Object, Vector3.zero, new Quaternion(0, 0, 0, 1), _Parent);
    }

    void SpawnObject(GameObject _Object, Transform _Parent, Quaternion _Rotation)
    {
        Instantiate(_Object, Vector3.zero, _Rotation, _Parent);
    }

    void SpawnObject(GameObject _Object, Transform _Parent, Vector3 _Position)
    {
        Instantiate(_Object, _Position, new Quaternion(0, 0, 0, 1), _Parent);
    }

    void SpawnObject(GameObject _Object, Transform _Parent, Vector3 _Position, Quaternion _Rotation)
    {
        Instantiate(_Object, _Position, _Rotation, _Parent);
    }
}
