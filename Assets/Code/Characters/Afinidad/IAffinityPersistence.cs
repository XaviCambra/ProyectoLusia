using System.Collections.Generic;

public interface IAffinityPersistence
{
    /// <summary>
    /// Guarda el estado completo de afinidad indexado por (fromId, toId).
    /// </summary>
    void Save(IReadOnlyDictionary<(string fromId, string toId), (int points, string trackId)> data);

    /// <summary>
    /// Carga el estado guardado.
    /// Devuelve null si no existe archivo (mantener estado inicial del SO).
    /// Devuelve diccionario vacío si el archivo existe pero no tiene entradas (reset explícito).
    /// </summary>
    Dictionary<(string fromId, string toId), (int points, string trackId)> Load();
}
