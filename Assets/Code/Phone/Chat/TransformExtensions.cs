using UnityEngine;

public static class TransformExtensions
{
    /// <summary>Destruye todos los hijos directos de este Transform.</summary>
    public static void DestroyAllChildren(this Transform parent)
    {
        foreach (Transform child in parent)
            Object.Destroy(child.gameObject);
    }
}
