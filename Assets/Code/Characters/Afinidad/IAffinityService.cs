using System;
using System.Collections.Generic;

/// <summary>
/// Un personaje conocido por otro, con su relacion ya resuelta en cada
/// direccion. Outgoing/Incoming es null si esa direccion concreta no es
/// conocida (aunque el dato de afinidad exista) — ver IAffinityService.IsKnown.
/// Devuelto por GetKnownCounterparts para que el llamador (tipicamente una
/// pantalla de "contactos") no tenga que volver a consultar el servicio.
/// </summary>
public readonly struct KnownContact
{
    public CharacterDefinition  Character { get; }
    public AffinityRelationship Outgoing  { get; }
    public AffinityRelationship Incoming  { get; }

    public KnownContact(CharacterDefinition character, AffinityRelationship outgoing, AffinityRelationship incoming)
    {
        Character = character;
        Outgoing  = outgoing;
        Incoming  = incoming;
    }
}

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

    /// <summary>
    /// True si 'from' ya conoce esta relacion (aunque los puntos/track ya existan
    /// como dato precableado de diseno). Por defecto false hasta que se llame a
    /// SetKnown — pensado para separar "el dato existe" de "el jugador lo sabe".
    /// </summary>
    bool IsKnown(CharacterDefinition from, CharacterDefinition to);

    /// <summary>
    /// Todos los personajes con relacion CONOCIDA con 'character', en cualquier
    /// direccion (el sabe de ellos, o ellos saben de el, o ambas), con esa
    /// relacion ya resuelta. Sin duplicados. Pensado para pantallas tipo "lista
    /// de contactos" que no deben calcular esto por su cuenta — la logica de
    /// que cuenta como "conocido" vive aqui, en un solo sitio.
    /// </summary>
    IEnumerable<KnownContact> GetKnownCounterparts(CharacterDefinition character);

    // --- Modificación ---

    /// <summary>Establece los puntos exactos (clampeado al rango del track actual). Preserva el track actual.</summary>
    void SetPoints(CharacterDefinition from, CharacterDefinition to, int points);

    /// <summary>Añade delta a los puntos actuales (clampeado). Devuelve el nuevo valor.</summary>
    int  AddPoints(CharacterDefinition from, CharacterDefinition to, int delta);

    /// <summary>Cambia el track de la relación sin alterar los puntos.</summary>
    void SetTrack(CharacterDefinition from, CharacterDefinition to, string trackId);

    /// <summary>Marca (o desmarca) si 'from' conoce esta relacion. Unico punto de entrada publico para cambiar IsKnown.</summary>
    void SetKnown(CharacterDefinition from, CharacterDefinition to, bool known);

    // --- Eventos ---

    /// <summary>Se dispara siempre que cambian los puntos entre dos personajes.</summary>
    event Action<AffinityChangedArgs>      OnAffinityChanged;

    /// <summary>Se dispara solo cuando el cambio de puntos cruza un umbral de nivel.</summary>
    event Action<AffinityRelationshipChangedArgs> OnRelationshipChanged;

    /// <summary>Se dispara cuando cambia IsKnown para un par (revelado u ocultado).</summary>
    event Action<AffinityKnownChangedArgs> OnKnownChanged;

    // --- Persistencia ---

    void Save();
    void Load();

    /// <summary>Borra todo el estado runtime. Los datos del SO de diseño no se modifican.</summary>
    void ResetAll();
}
