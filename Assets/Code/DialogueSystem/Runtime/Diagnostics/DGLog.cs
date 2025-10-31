using UnityEngine;

public static class DGLog
{
    private const bool AllowLogsForDebbug = false;
    public static void Info(string msg, Object ctx = null)
    {
        if (!AllowLogsForDebbug) return;
        Debug.Log("[DG] " + msg, ctx);
    }
    public static void Warn(string msg, Object ctx = null)
    {
        if (!AllowLogsForDebbug) return;
        Debug.LogWarning("[DG] " + msg, ctx);
    }
    public static void Err(string msg, Object ctx = null)
    {
        if (!AllowLogsForDebbug) return;
        Debug.LogError("[DG] " + msg, ctx);
    }
}
