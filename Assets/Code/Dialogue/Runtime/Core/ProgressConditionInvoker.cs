// Assets/Dialogue/Runtime/Core/ProgressConditionInvoker.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public static class ProgressConditionInvoker
{
    // Registro: nombre -> funci�n (arg -> bool)
    private static readonly Dictionary<string, Func<object, bool>> _registry = new();

    /// <summary>Registra un m�todo evaluador de progreso por nombre.</summary>
    public static void Register(string methodName, Func<object, bool> fn)
    {
        if (string.IsNullOrWhiteSpace(methodName) || fn == null) return;
        _registry[methodName] = fn;
    }

    /// <summary>Desregistra un m�todo previamente registrado.</summary>
    public static void Unregister(string methodName)
    {
        if (string.IsNullOrWhiteSpace(methodName)) return;
        _registry.Remove(methodName);
    }

    /// <summary>
    /// Intenta invocar el evaluador. Devuelve true si existe y se invoca sin excepci�n.
    /// </summary>
    public static bool TryInvoke(string methodName, object arg, out bool result)
    {
        result = false;
        if (string.IsNullOrWhiteSpace(methodName)) return false;

        if (_registry.TryGetValue(methodName, out var fn))
        {
            try
            {
                result = fn.Invoke(arg);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>Vac�a el registro (�til en tests o recargas).</summary>
    public static void Clear() => _registry.Clear();
}
