// Runtime/Config/ParamSettings.cs
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Config/Param Settings", fileName = "ParamSettings")]
public class ParamSettings : ScriptableObject
{
    [Serializable] public struct FloatParam { public string key; public float value; }

    [Header("Floats")]
    public List<FloatParam> floats = new();

    public bool TryGetFloat(string key, out float v)
    {
        var found = floats.FirstOrDefault(p => p.key == key);
        if (!string.IsNullOrEmpty(found.key)) { v = found.value; return true; }
        v = default; return false;
    }
}
