using System;
using System.Collections.Generic;

/// <summary>
/// Implementación de IAffinityService con lookups O(1) mediante Dictionary.
/// Todo el estado mutable vive aquí en memoria — el CharacterAffinityMap SO es solo
/// datos de diseño (estado inicial) y nunca se modifica en runtime.
/// </summary>
public sealed class AffinityMapService : IAffinityService
{
    private readonly AffinitySchema       _schema;
    private readonly CharacterDatabase    _db;
    private readonly AffinitySymmetry     _symmetry;
    private readonly IAffinityPersistence _persistence;

    // Estado runtime — clave: (fromId GUID, toId GUID) → puntos
    private readonly Dictionary<(string, string), int> _data;

    public event Action<AffinityChangedArgs>      OnAffinityChanged;
    public event Action<AffinityLevelChangedArgs> OnLevelChanged;

    public AffinityMapService(
        CharacterAffinityMap  map,
        CharacterDatabase     db,
        IAffinityPersistence  persistence = null)
    {
        _schema      = map.schema;
        _db          = db;
        _symmetry    = map.symmetry;
        _persistence = persistence;
        _data        = new Dictionary<(string, string), int>(64);

        InitializeFromMap(map);
    }

    // -----------------------------------------------------------------------
    // Consulta
    // -----------------------------------------------------------------------

    public int GetPoints(CharacterDefinition from, CharacterDefinition to)
    {
        if (!from || !to) return DefaultPoints;
        return _data.TryGetValue(Key(from, to), out int p) ? p : DefaultPoints;
    }

    public AffinityBand GetLevel(CharacterDefinition from, CharacterDefinition to)
        => _schema?.GetBandForPoints(GetPoints(from, to));

    public bool HasRelationship(CharacterDefinition from, CharacterDefinition to)
    {
        if (!from || !to) return false;
        return _data.ContainsKey(Key(from, to));
    }

    public IEnumerable<(CharacterDefinition to, int points, AffinityBand level)>
        GetRelationshipsFrom(CharacterDefinition from)
    {
        if (!from) yield break;
        string fromId = from.CharacterId;
        foreach (var kvp in _data)
        {
            if (kvp.Key.Item1 != fromId) continue;
            var to = _db.GetById(kvp.Key.Item2);
            if (!to) continue;
            yield return (to, kvp.Value, _schema?.GetBandForPoints(kvp.Value));
        }
    }

    public IEnumerable<(CharacterDefinition from, int points, AffinityBand level)>
        GetRelationshipsTo(CharacterDefinition to)
    {
        if (!to) yield break;
        string toId = to.CharacterId;
        foreach (var kvp in _data)
        {
            if (kvp.Key.Item2 != toId) continue;
            var from = _db.GetById(kvp.Key.Item1);
            if (!from) continue;
            yield return (from, kvp.Value, _schema?.GetBandForPoints(kvp.Value));
        }
    }

    // -----------------------------------------------------------------------
    // Modificación
    // -----------------------------------------------------------------------

    public void SetPoints(CharacterDefinition from, CharacterDefinition to, int points)
    {
        if (!from || !to) return;

        int          oldPoints = GetPoints(from, to);
        AffinityBand oldLevel  = GetLevel(from, to);
        int          clamped   = Clamp(points);

        Write(from, to, clamped);

        FireEvents(from, to, oldPoints, clamped, oldLevel);
    }

    public int AddPoints(CharacterDefinition from, CharacterDefinition to, int delta)
    {
        if (!from || !to) return GetPoints(from, to);

        int          oldPoints = GetPoints(from, to);
        AffinityBand oldLevel  = GetLevel(from, to);
        int          clamped   = Clamp(oldPoints + delta);

        Write(from, to, clamped);

        FireEvents(from, to, oldPoints, clamped, oldLevel);
        return clamped;
    }

    // -----------------------------------------------------------------------
    // Persistencia
    // -----------------------------------------------------------------------

    public void Save()  => _persistence?.Save(_data);

    public void Load()
    {
        if (_persistence == null) return;

        var saved = _persistence.Load();
        if (saved == null) return; // sin archivo → mantener estado inicial del SO

        _data.Clear();
        foreach (var kvp in saved)
            _data[kvp.Key] = Clamp(kvp.Value);
    }

    public void ResetAll() => _data.Clear();

    // -----------------------------------------------------------------------
    // Internos
    // -----------------------------------------------------------------------

    private int DefaultPoints => _schema?.defaultPoints ?? 0;

    private static (string, string) Key(CharacterDefinition from, CharacterDefinition to)
        => (from.CharacterId, to.CharacterId);

    private int Clamp(int points)
    {
        if (_schema == null) return points;
        return Math.Clamp(points, _schema.globalMin, _schema.globalMax);
    }

    private void Write(CharacterDefinition from, CharacterDefinition to, int points)
    {
        _data[Key(from, to)] = points;
        if (_symmetry == AffinitySymmetry.Mirror)
            _data[Key(to, from)] = points;
    }

    private void FireEvents(CharacterDefinition from, CharacterDefinition to,
                            int oldPoints, int newPoints, AffinityBand oldLevel)
    {
        if (oldPoints == newPoints) return;

        OnAffinityChanged?.Invoke(new AffinityChangedArgs(from, to, oldPoints, newPoints));

        AffinityBand newLevel = GetLevel(from, to);
        if (oldLevel?.name != newLevel?.name)
            OnLevelChanged?.Invoke(new AffinityLevelChangedArgs(from, to, oldLevel, newLevel));
    }

    private void InitializeFromMap(CharacterAffinityMap map)
    {
        foreach (var entry in map.InitialEntries)
        {
            if (!entry.from || !entry.to) continue;
            int clamped = Clamp(entry.points);
            _data[Key(entry.from, entry.to)] = clamped;
            if (_symmetry == AffinitySymmetry.Mirror && !_data.ContainsKey(Key(entry.to, entry.from)))
                _data[Key(entry.to, entry.from)] = clamped;
        }
    }
}
