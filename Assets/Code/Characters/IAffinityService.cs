using System;
using System.Collections.Generic;

public interface IAffinityService
{
    // --- Consulta ---

    /// <summary>Puntos de afinidad de 'from' hacia 'to'. Devuelve defaultPoints si no existe relación.</summary>
    int          GetPoints(CharacterDefinition from, CharacterDefinition to);

    /// <summary>Nivel de afinidad según el AffinitySchema. Null si no hay schema o puntos fuera de rango.</summary>
    AffinityBand GetLevel(CharacterDefinition from, CharacterDefinition to);

    /// <summary>True solo si existe una entrada registrada para este par.</summary>
    bool         HasRelationship(CharacterDefinition from, CharacterDefinition to);

    /// <summary>Todas las relaciones que 'from' tiene hacia otros personajes.</summary>
    IEnumerable<(CharacterDefinition to, int points, AffinityBand level)>
        GetRelationshipsFrom(CharacterDefinition from);

    /// <summary>Todas las relaciones de otros personajes hacia 'to'.</summary>
    IEnumerable<(CharacterDefinition from, int points, AffinityBand level)>
        GetRelationshipsTo(CharacterDefinition to);

    // --- Modificación ---

    /// <summary>Establece los puntos exactos (clampeado a globalMin/globalMax).</summary>
    void SetPoints(CharacterDefinition from, CharacterDefinition to, int points);

    /// <summary>Añade delta a los puntos actuales (clampeado). Devuelve el nuevo valor.</summary>
    int  AddPoints(CharacterDefinition from, CharacterDefinition to, int delta);

    // --- Eventos ---

    /// <summary>Se dispara siempre que cambian los puntos entre dos personajes.</summary>
    event Action<AffinityChangedArgs>      OnAffinityChanged;

    /// <summary>Se dispara solo cuando el cambio de puntos cruza un umbral de nivel.</summary>
    event Action<AffinityLevelChangedArgs> OnLevelChanged;

    // --- Persistencia ---

    void Save();
    void Load();

    /// <summary>Borra todo el estado runtime. Los datos del SO de diseño no se modifican.</summary>
    void ResetAll();
}
