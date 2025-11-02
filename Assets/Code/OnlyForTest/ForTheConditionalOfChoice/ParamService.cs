// Runtime/Services/ParamService.cs
using UnityEngine;

public static class ParamService
{
    private static ParamSettings _settings;

    /// <summary>
    /// (Opcional) Inicia con una referencia directa si no quieres usar Resources.
    /// </summary>
    public static void Init(ParamSettings settings) => _settings = settings;

    private static ParamSettings Settings
    {
        get
        {
            if (_settings == null)
            {
                // Carga perezosa: coloca un asset llamado "ParamSettings" dentro de una carpeta Resources/
                _settings = Resources.Load<ParamSettings>("ParamSettings/ParamSettingsTest");
                if (_settings == null)
                    Debug.LogWarning("[ParamService] No se encontró Resources/ParamSettings. Devolviendo valores por defecto.");
            }
            return _settings;
        }
    }

    /// <summary> Devuelve un float paramétrico. </summary>
    public static float GetFloat(string key, float @default = 0f)
    {
        if (Settings != null && Settings.TryGetFloat(key, out var v)) return v;
        return @default;
    }

    /// <summary> Devuelve un color paramétrico. </summary>
    public static Color GetColor(string key, Color defaultColor)
    {
        if (Settings != null && Settings.TryGetColor(key, out var v)) return v;
        return defaultColor;
    }

    /// <summary> Devuelve una curva paramétrica. </summary>
    public static AnimationCurve GetCurve(string key, AnimationCurve defaultCurve = null)
    {
        if (Settings != null && Settings.TryGetCurve(key, out var v) && v != null) return v;
        return defaultCurve ?? AnimationCurve.Linear(0, 0, 1, 1);
    }
}
