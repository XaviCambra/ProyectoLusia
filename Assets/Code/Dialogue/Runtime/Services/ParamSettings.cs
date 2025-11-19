// Runtime/Config/ParamSettings.cs
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Config/Param Settings", fileName = "ParamSettings")]
public class ParamSettings : ScriptableObject
{
    [Serializable] public struct FloatParam { public string key; public float value; }
    [Serializable] public struct ColorParam { public string key; public Color value; }
    [Serializable] public struct CurveParam { public string key; public AnimationCurve value; }

    [Header("Floats")]
    public List<FloatParam> floats = new();

    [Header("Colors")]
    public List<ColorParam> colors = new();

    [Header("Curves")]
    public List<CurveParam> curves = new();

    public bool TryGetFloat(string key, out float v)
    {
        var found = floats.FirstOrDefault(p => p.key == key);
        if (!string.IsNullOrEmpty(found.key)) { v = found.value; return true; }
        v = default; return false;
    }

    public bool TryGetColor(string key, out Color v)
    {
        var found = colors.FirstOrDefault(p => p.key == key);
        if (!string.IsNullOrEmpty(found.key)) { v = found.value; return true; }
        v = default; return false;
    }

    public bool TryGetCurve(string key, out AnimationCurve v)
    {
        var found = curves.FirstOrDefault(p => p.key == key);
        if (!string.IsNullOrEmpty(found.key)) { v = found.value; return true; }
        v = null; return false;
    }
}
