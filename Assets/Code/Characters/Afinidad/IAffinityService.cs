using System;

public interface IAffinityService
{
    // --- Schema ---

    /// <summary>Acceso al schema de afinidad: tracks disponibles, bandas y sus rangos de puntos.</summary>
    AffinitySchema Schema { get; }

    // --- Consulta ---

    /// <summary>Puntos de afinidad de 'from' hacia 'to'. Devuelve defaultPoints si no existe relación.</summary>
    int GetPoints(CharacterDefinition from, CharacterDefinition to);

    /// <summary>Nivel de afinidad según el track asignado al par. Null si no hay schema o puntos fuera de rango.</summary>
    AffinityRelationship GetRelationship(CharacterDefinition from, CharacterDefinition to);

    /// <summary>True solo si existe una entrada registrada para este par.</summary>
    bool HasRelationship(CharacterDefinition from, CharacterDefinition to);

    /// <summary>Track de relación activo para este par. Devuelve defaultTrackId si no hay entrada.</summary>
    string GetTrack(CharacterDefinition from, CharacterDefinition to);

    // --- Modificación ---

    /// <summary>Establece los puntos exactos (clampeado a globalMin/globalMax). Preserva el track actual.</summary>
    void SetPoints(CharacterDefinition from, CharacterDefinition to, int points);

    /// <summary>Añade delta a los puntos actuales (clampeado). Devuelve el nuevo valor.</summary>
    int  AddPoints(CharacterDefinition from, CharacterDefinition to, int delta);

    /// <summary>Cambia el track de la relación sin alterar los puntos.</summary>
    void SetTrack(CharacterDefinition from, CharacterDefinition to, string trackId);

    // --- Eventos ---

    /// <summary>Se dispara siempre que cambian los puntos entre dos personajes.</summary>
    event Action<AffinityChangedArgs>      OnAffinityChanged;

    /// <summary>Se dispara solo cuando el cambio de puntos cruza un umbral de nivel.</summary>
    event Action<AffinityRelationshipChangedArgs> OnRelationshipChanged;

    // --- Persistencia ---

    void Save();
    void Load();

    /// <summary>Borra todo el estado runtime. Los datos del SO de diseño no se modifican.</summary>
    void ResetAll();
}
