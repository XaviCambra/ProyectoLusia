using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class JsonAffinityPersistence : IAffinityPersistence
{
    private readonly string _path;

    public JsonAffinityPersistence(string fileName = "affinity.json")
        => _path = Path.Combine(Application.persistentDataPath, fileName);

    public void Save(IReadOnlyDictionary<(string fromId, string toId), (int points, string trackId)> data)
    {
        var dto = new SaveData();
        foreach (var kvp in data)
            dto.records.Add(new SaveData.RecordDto
            {
                fromId  = kvp.Key.fromId,
                toId    = kvp.Key.toId,
                points  = kvp.Value.points,
                trackId = kvp.Value.trackId
            });

        File.WriteAllText(_path, JsonUtility.ToJson(dto, prettyPrint: true));
    }

    public Dictionary<(string fromId, string toId), (int points, string trackId)> Load()
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

        var result = new Dictionary<(string, string), (int, string)>(dto.records?.Count ?? 0);
        if (dto.records == null) return result;

        foreach (var r in dto.records)
        {
            if (string.IsNullOrEmpty(r.fromId) || string.IsNullOrEmpty(r.toId)) continue;
            result[(r.fromId, r.toId)] = (r.points, r.trackId ?? "default");
        }
        return result;
    }

    [Serializable]
    private class SaveData
    {
        public List<RecordDto> records = new();

        [Serializable]
        public class RecordDto
        {
            public string fromId;
            public string toId;
            public int    points;
            public string trackId;
        }
    }
}
