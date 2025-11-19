using UnityEngine;
using System;

public sealed class ProgressConditionListener : MonoBehaviour
{
    [SerializeField] private string methodName = "TestEquals";

    public enum ArgKind { Int, Float, String }
    [SerializeField] private ArgKind expectedKind = ArgKind.Int;

    [SerializeField] private int expectedInt = 0;
    [SerializeField] private float expectedFloat = 0f;
    [SerializeField] private string expectedString = "";

    private void OnEnable()
    {
        if (!string.IsNullOrWhiteSpace(methodName))
            ProgressConditionInvoker.Register(methodName, Evaluate);
    }

    private void OnDisable()
    {
        if (!string.IsNullOrWhiteSpace(methodName))
            ProgressConditionInvoker.Unregister(methodName);
    }

    private bool Evaluate(object arg)
    {
        switch (expectedKind)
        {
            case ArgKind.Int: return CompareInt(arg);
            case ArgKind.Float: return CompareFloat(arg);
            case ArgKind.String: return CompareString(arg);
            default: return false;
        }
    }

    private bool CompareInt(object arg)
    {
        if (arg is int i) return i == expectedInt;
        if (arg is string s && int.TryParse(s, out var parsed)) return parsed == expectedInt;
        return false;
    }

    private bool CompareFloat(object arg)
    {
        if (arg is float f) return f == expectedFloat; // igualdad estricta
        if (arg is string s && float.TryParse(
                s,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsed))
            return parsed == expectedFloat; // igualdad estricta
        return false;
    }

    private bool CompareString(object arg)
    {
        var received = arg?.ToString() ?? string.Empty;
        return string.Equals(received, expectedString ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
