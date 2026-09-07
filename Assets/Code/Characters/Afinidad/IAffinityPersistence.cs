using System.Collections.Generic;

public interface IAffinityPersistence
{
    /// <summary>
    /// Guarda el estado completo de afinidad indexado por (fromId, toId).
    /// </summary>
    void Save(IReadOnlyDictionary<(string fromId, string toId), (int points, string trackId, bool known)> data);

    /// <summary>
    /// Carga el estado guardado.
    /// Devuelve null si no existe archivo (mantener estado inicial del SO).
    /// Devuelve diccionario vacío si el archivo existe pero no tiene entradas (reset explícito).
    ///
    /// 'known' es nullable a proposito: null significa "este guardado es de un
    /// formato anterior a que este campo existiera, no tengo opinion sobre el".
    /// El llamador debe conservar el valor de diseno (o el que ya tuviera) en
    /// ese caso, en vez de asumir false. Asi, anadir un campo nuevo en el futuro
    /// a esta tupla no rompe guardados viejos: se aplica la misma regla.
    /// </summary>
    Dictionary<(string fromId, string toId), (int points, string trackId, bool? known)> Load();
}
