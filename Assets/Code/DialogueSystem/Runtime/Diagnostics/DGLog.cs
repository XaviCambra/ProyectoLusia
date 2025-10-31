using UnityEngine;

public static class DGLog
{
    public static void Info(string msg, Object ctx = null) => Debug.Log("[DG] " + msg, ctx);
    public static void Warn(string msg, Object ctx = null) => Debug.LogWarning("[DG] " + msg, ctx);
    public static void Err(string msg, Object ctx = null) => Debug.LogError("[DG] " + msg, ctx);
}
