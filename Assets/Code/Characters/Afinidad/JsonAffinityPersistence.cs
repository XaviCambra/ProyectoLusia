using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class JsonAffinityPersistence : IAffinityPersistence
{
    // Version del formato de guardado. Subela cada vez que anadas un campo
    // nuevo a RecordDto, y gatea su lectura en Load() igual que 'known' aqui
    // abajo (KnownFieldVersion) — asi un guardado antiguo nunca pisa el campo
    // nuevo con un valor por defecto falso; se conserva el valor de diseno.
    private const int CurrentVersion   = 2;
    private const int KnownFieldVersion = 2; // version en la que se anadio 'known'

    private readonly string _path;

    /// <summary>Ruta completa del archivo de guardado. Publica solo para herramientas de Editor (ver AffinitySaveMenu).</summary>
    public string FilePath => _path;

    public JsonAffinityPersistence(string fileName = "affinity.json")
        => _path = Path.Combine(Application.persistentDataPath, fileName);

    public void Save(IReadOnlyDictionary<(string fromId, string toId), (int points, string trackId, bool known)> data)
    {
        var dto = new SaveData { version = CurrentVersion };
        foreach (var kvp in data)
            dto.records.Add(new SaveData.RecordDto
            {
                fromId  = kvp.Key.fromId,
                toId    = kvp.Key.toId,
                points  = kvp.Value.points,
                trackId = kvp.Value.trackId,
                known   = kvp.Value.known
            });

        File.WriteAllText(_path, JsonUtility.ToJson(dto, prettyPrint: true));
    }

    public Dictionary<(string fromId, string toId), (int points, string trackId, bool? known)> Load()
    {
        if (!File.Exists(_path)) return null;

        SaveData dto;
        try
        {
            dto = JsonUtility.FromJson<SaveData>(File.ReadAllText(_path));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[JsonAffinityPersistence] Error al leer {_path}: {e.Message}");
            return null;
        }

        var result = new Dictionary<(string, string), (int, string, bool?)>(dto.records?.Count ?? 0);
        if (dto.records == null) return result;

        // JsonUtility no distingue "el campo no estaba en el JSON" de "estaba
        // en false": por eso la version manda, no el valor leido del campo.
        bool hasKnownField = dto.version >= KnownFieldVersion;

        foreach (var r in dto.records)
        {
            if (string.IsNullOrEmpty(r.fromId) || string.IsNullOrEmpty(r.toId)) continue;
            bool? known = hasKnownField ? r.known : null;
            result[(r.fromId, r.toId)] = (r.points, r.trackId ?? "default", known);
        }
        return result;
    }

    [Serializable]
    private class SaveData
    {
        public int version;
        public List<RecordDto> records = new();

        [Serializable]
        public class RecordDto
        {
            public string fromId;
            public string toId;
            public int    points;
            public string trackId;
            public bool   known;
        }
    }
}
