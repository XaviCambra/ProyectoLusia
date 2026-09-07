using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Implementacion de IAffinityService con lookups O(1) mediante Dictionary.
/// Cada par almacena (points, trackId, known) — el track determina que linea de
/// progreso sigue la relacion, known si 'from' ya la conoce (ver SetKnown).
/// El CharacterAffinityMap SO es solo datos de diseno (estado inicial) y nunca se modifica en runtime.
/// </summary>
public sealed class AffinityMapService : IAffinityService
{
    private readonly AffinitySchema       _schema;
    private readonly IAffinityPersistence _persistence;

    // Estado runtime — clave: (fromId, toId) -> (puntos, trackId, conocida)
    private readonly Dictionary<(string, string), (int points, string trackId, bool known)> _data;

    // Registro inverso id -> personaje, solo para poder devolver CharacterDefinition
    // desde GetKnownCounterparts (_data solo guarda ids, no referencias). Se llena
    // una vez al arrancar, a la vez que _data.
    private readonly Dictionary<string, CharacterDefinition> _charactersById;

    public event Action<AffinityChangedArgs>      OnAffinityChanged;
    public event Action<AffinityRelationshipChangedArgs> OnRelationshipChanged;
    public event Action<AffinityKnownChangedArgs> OnKnownChanged;

    public AffinityMapService(
        CharacterAffinityMap  map,
        IAffinityPersistence  persistence = null)
    {
        _schema          = map.schema;
        _persistence     = persistence;
        _data            = new Dictionary<(string, string), (int, string, bool)>(64);
        _charactersById  = new Dictionary<string, CharacterDefinition>(32);

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

    public AffinityRelationship GetRelationship(CharacterDefinition from, CharacterDefinition to)
    {
        if (!from || !to) return null;
        var trackId = _data.TryGetValue(Key(from, to), out var s) ? s.trackId : DefaultTrackId;
        return _schema?.GetRelationshipForPoints(GetPoints(from, to), trackId);
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

    public bool IsKnown(CharacterDefinition from, CharacterDefinition to)
    {
        if (!from || !to) return false;
        return _data.TryGetValue(Key(from, to), out var s) && s.known;
    }

    public IEnumerable<KnownContact> GetKnownCounterparts(CharacterDefinition character)
    {
        if (!character) yield break;

        var id   = character.CharacterId;
        var seen = new HashSet<string>();

        foreach (var key in _data.Keys)
        {
            // El par puede tener a 'character' como origen o como destino: en
            // cualquiera de los dos casos el "otro" personaje es un candidato.
            string otherId = key.Item1 == id ? key.Item2
                           : key.Item2 == id ? key.Item1
                           : null;

            if (otherId == null || otherId == id) continue;   // no involucra a 'character', o es autoreferencia
            if (!seen.Add(otherId)) continue;                 // ya procesado (aparece en las dos direcciones)
            if (!_charactersById.TryGetValue(otherId, out var other)) continue;

            // IsKnown/GetRelationship son la fuente de verdad de cada direccion:
            // se reutilizan aqui en vez de leer 'entry.known' a mano, para no
            // duplicar esa logica.
            bool outgoingKnown = IsKnown(character, other);
            bool incomingKnown = IsKnown(other, character);
            if (!outgoingKnown && !incomingKnown) continue;

            yield return new KnownContact(
                other,
                outgoingKnown ? GetRelationship(character, other) : null,
                incomingKnown ? GetRelationship(other, character) : null);
        }
    }

    // -----------------------------------------------------------------------
    // Modificacion
    // -----------------------------------------------------------------------

    public void SetPoints(CharacterDefinition from, CharacterDefinition to, int points)
    {
        if (!from || !to) return;

        int          oldPoints = GetPoints(from, to);
        AffinityRelationship oldLevel  = GetRelationship(from, to);
        string       trackId   = GetTrack(from, to);
        int          clamped   = Clamp(points, trackId);

        Write(from, to, clamped);
        FireEvents(from, to, oldPoints, clamped, oldLevel);
    }

    public int AddPoints(CharacterDefinition from, CharacterDefinition to, int delta)
    {
        if (!from || !to) return GetPoints(from, to);

        int          oldPoints = GetPoints(from, to);
        AffinityRelationship oldLevel  = GetRelationship(from, to);
        string       trackId   = GetTrack(from, to);
        int          clamped   = Clamp(oldPoints + delta, trackId);

        Write(from, to, clamped);
        FireEvents(from, to, oldPoints, clamped, oldLevel);
        return clamped;
    }

    public void SetTrack(CharacterDefinition from, CharacterDefinition to, string trackId)
    {
        if (!from || !to) return;
        string resolved = string.IsNullOrEmpty(trackId) ? DefaultTrackId : trackId;
        var    key      = Key(from, to);
        var    existing = GetOrDefault(key, resolved);
        // El nuevo track puede tener un rango distinto: reclampeamos para no dejar
        // puntos fuera de rango (y por tanto sin nivel/color) tras el cambio de track.
        _data[key] = (Clamp(existing.points, resolved), resolved, existing.known);
    }

    /// <summary>Unico punto de entrada para cambiar IsKnown. Preserva puntos y track.</summary>
    public void SetKnown(CharacterDefinition from, CharacterDefinition to, bool known)
    {
        if (!from || !to) return;
        var key      = Key(from, to);
        var existing = GetOrDefault(key, DefaultTrackId);
        if (existing.known == known) return;

        _data[key] = (existing.points, existing.trackId, known);
        OnKnownChanged?.Invoke(new AffinityKnownChangedArgs(from, to, known));
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

        // OJO: no hacemos _data.Clear(). Los valores de diseno ya cargados por
        // InitializeFromMap se quedan como base y el guardado los pisa por
        // encima solo para los pares que contiene. Asi, una relacion nueva
        // anadida al CharacterAffinityMap despues de que ya exista un guardado
        // no desaparece — sigue con su valor de diseno hasta que se guarde.
        foreach (var kvp in saved)
        {
            // Ignora registros huerfanos: si alguno de los dos ids ya no
            // corresponde a un personaje del diseno actual (por ejemplo, el
            // CharacterId de un personaje cambio, o el personaje se borro),
            // no se aplica. Sin esto, un guardado viejo puede resucitar para
            // siempre pares fantasma que ya no existen.
            if (!_charactersById.ContainsKey(kvp.Key.Item1) || !_charactersById.ContainsKey(kvp.Key.Item2))
                continue;

            string trackId = kvp.Value.trackId ?? DefaultTrackId;
            // known es null si el guardado es de un formato anterior a que
            // 'known' existiera: en ese caso no lo tocamos, se queda el valor
            // de diseno (o el que ya hubiera) en vez de caer a false por defecto.
            bool known = kvp.Value.known ?? (_data.TryGetValue(kvp.Key, out var existing) ? existing.known : false);
            _data[kvp.Key] = (Clamp(kvp.Value.points, trackId), trackId, known);
        }
    }

    public void ResetAll() => _data.Clear();

    // -----------------------------------------------------------------------
    // Internos
    // -----------------------------------------------------------------------

    private string DefaultTrackId => _schema?.Tracks.FirstOrDefault()?.id ?? "default";

    private static (string, string) Key(CharacterDefinition from, CharacterDefinition to)
        => (from.CharacterId, to.CharacterId);

    private int Clamp(int points, string trackId)
    {
        var track = _schema?.GetTrack(trackId);
        if (track == null) return points;
        return Math.Clamp(points, track.MinPoints, track.MaxPoints);
    }

    // Write preserva el trackId y el known existentes; solo actualiza los puntos.
    private void Write(CharacterDefinition from, CharacterDefinition to, int points)
    {
        var key      = Key(from, to);
        var existing = GetOrDefault(key, DefaultTrackId);
        _data[key] = (points, existing.trackId, existing.known);
    }

    // Valor por defecto (0 puntos, sin conocer) para un par sin entrada aun.
    private (int points, string trackId, bool known) GetOrDefault((string, string) key, string fallbackTrackId)
        => _data.TryGetValue(key, out var s) ? s : (points: 0, trackId: fallbackTrackId, known: false);

    private void FireEvents(CharacterDefinition from, CharacterDefinition to,
                            int oldPoints, int newPoints, AffinityRelationship oldLevel)
    {
        if (oldPoints == newPoints) return;

        OnAffinityChanged?.Invoke(new AffinityChangedArgs(from, to, oldPoints, newPoints));

        AffinityRelationship newLevel = GetRelationship(from, to);
        if (oldLevel?.name != newLevel?.name)
            OnRelationshipChanged?.Invoke(new AffinityRelationshipChangedArgs(from, to, oldLevel, newLevel));
    }

    private void InitializeFromMap(CharacterAffinityMap map)
    {
        foreach (var entry in map.InitialEntries)
        {
            if (!entry.from || !entry.to) continue;

            _charactersById[entry.from.CharacterId] = entry.from;
            _charactersById[entry.to.CharacterId]   = entry.to;

            string track   = string.IsNullOrEmpty(entry.trackId) ? DefaultTrackId : entry.trackId;
            int    clamped = Clamp(entry.points, track);
            _data[Key(entry.from, entry.to)] = (clamped, track, entry.known);
        }
    }
}
