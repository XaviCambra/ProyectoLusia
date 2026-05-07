using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Implementación de IAffinityService con lookups O(1) mediante Dictionary.
/// Cada par almacena (points, trackId) — el track determina qué línea de progreso sigue la relación.
/// El CharacterAffinityMap SO es solo datos de diseño (estado inicial) y nunca se modifica en runtime.
/// </summary>
public sealed class AffinityMapService : IAffinityService
{
    private readonly AffinitySchema       _schema;
    private readonly IAffinityPersistence _persistence;

    // Estado runtime — clave: (fromId, toId) → (puntos, trackId)
    private readonly Dictionary<(string, string), (int points, string trackId)> _data;

    public event Action<AffinityChangedArgs>      OnAffinityChanged;
    public event Action<AffinityLevelChangedArgs> OnLevelChanged;

    public AffinityMapService(
        CharacterAffinityMap  map,
        IAffinityPersistence  persistence = null)
    {
        _schema      = map.schema;
        _persistence = persistence;
        _data        = new Dictionary<(string, string), (int, string)>(64);

        InitializeFromMap(map);
    }

    public AffinitySchema Schema => _schema;

    // -----------------------------------------------------------------------
    // Consulta
    // -----------------------------------------------------------------------

    public int GetPoints(CharacterDefinition from, CharacterDefinition to)
    {
        if (!from || !to) return 0;
        return _data.TryGetValue(Key(from, to), out var s) ? s.points : 0;
    }

    public AffinityBand GetLevel(CharacterDefinition from, CharacterDefinition to)
    {
        if (!from || !to) return null;
        var trackId = _data.TryGetValue(Key(from, to), out var s) ? s.trackId : DefaultTrackId;
        return _schema?.GetBandForPoints(GetPoints(from, to), trackId);
    }

    public bool HasRelationship(CharacterDefinition from, CharacterDefinition to)
    {
        if (!from || !to) return false;
        return _data.ContainsKey(Key(from, to));
    }

    public string GetTrack(CharacterDefinition from, CharacterDefinition to)
    {
        if (!from || !to) return DefaultTrackId;
        return _data.TryGetValue(Key(from, to), out var s) ? s.trackId : DefaultTrackId;
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

    public void SetTrack(CharacterDefinition from, CharacterDefinition to, string trackId)
    {
        if (!from || !to) return;
        string resolved = string.IsNullOrEmpty(trackId) ? DefaultTrackId : trackId;
        var    key      = Key(from, to);
        int    points   = _data.TryGetValue(key, out var s) ? s.points : 0;
        _data[key] = (points, resolved);
    }

    // -----------------------------------------------------------------------
    // Persistencia
    // -----------------------------------------------------------------------

    public void Save()  => _persistence?.Save(_data);

    public void Load()
    {
        if (_persistence == null) return;

        var saved = _persistence.Load();
        if (saved == null) return;

        _data.Clear();
        foreach (var kvp in saved)
            _data[kvp.Key] = (Clamp(kvp.Value.points), kvp.Value.trackId ?? DefaultTrackId);
    }

    public void ResetAll() => _data.Clear();

    // -----------------------------------------------------------------------
    // Internos
    // -----------------------------------------------------------------------

    private string DefaultTrackId => _schema?.Tracks.FirstOrDefault()?.id ?? "default";

    private static (string, string) Key(CharacterDefinition from, CharacterDefinition to)
        => (from.CharacterId, to.CharacterId);

    private int Clamp(int points)
    {
        if (_schema == null) return points;
        return Math.Clamp(points, _schema.globalMin, _schema.globalMax);
    }

    // Write preserva el trackId existente; solo actualiza los puntos.
    private void Write(CharacterDefinition from, CharacterDefinition to, int points)
    {
        var key     = Key(from, to);
        var trackId = _data.TryGetValue(key, out var s) ? s.trackId : DefaultTrackId;
        _data[key] = (points, trackId);
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
            int    clamped = Clamp(entry.points);
            string track   = string.IsNullOrEmpty(entry.trackId) ? DefaultTrackId : entry.trackId;
            _data[Key(entry.from, entry.to)] = (clamped, track);
        }
    }
}
