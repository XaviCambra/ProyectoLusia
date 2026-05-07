using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ejemplos de uso del sistema de afinidad. Solo referencia — no es código de producción.
/// Asigna los personajes en el Inspector y asegúrate de tener un AffinityServiceBootstrapper en escena.
/// </summary>
public sealed class AffinityQueryExample : MonoBehaviour
{
    [SerializeField] private CharacterDefinition player;
    [SerializeField] private CharacterDefinition npcA;
    [SerializeField] private CharacterDefinition npcB;
    [SerializeField] private List<CharacterDefinition> grupo;

    // Acceso al servicio de afinidad. Es un singleton inicializado por AffinityServiceBootstrapper.
    // Si es null significa que el bootstrapper no está en la escena o aún no ha ejecutado su Awake.
    private IAffinityService Svc => AffinityServiceBootstrapper.Service;

    private void Start()
    {
        // ── Leer estado actual ───────────────────────────────────────────────
        // GetPoints  → devuelve los puntos numéricos de la relación de A hacia B.
        //              Si no existe la relación devuelve 0.
        // GetRelationship   → devuelve la AffinityRelationship activa (nombre, color, rango de puntos).
        //              Null si los puntos no caen en ninguna banda definida.
        // HasRelationship → true solo si la relación fue registrada en el CharacterAffinityMap.

        int          puntos = Svc.GetPoints(player, npcA);
        AffinityRelationship nivel  = Svc.GetRelationship(player, npcA);
        bool         existe = Svc.HasRelationship(player, npcA);

        Debug.Log($"{player.displayName} → {npcA.displayName}: {puntos} pts | Nivel: {nivel?.name} | Relación: {existe}");

        // ── Condiciones típicas en combate ───────────────────────────────────
        // Compara puntos directamente para activar lógica de juego.
        // Usa GetRelationship si prefieres comparar por nombre de banda en lugar de por número.

        if (Svc.GetPoints(player, npcA) >= 50)
            Debug.Log("Son aliados fuertes — se pueden pedir favores o refuerzos.");

        if (Svc.GetPoints(player, npcB) < 0)
            Debug.Log($"{player.displayName} y {npcB.displayName} son hostiles — pueden atacarse.");

        if (Svc.GetRelationship(player, npcA)?.name == "Aliado")
            Debug.Log("Están en banda Aliado — desbloquea diálogos especiales.");

        // ── Modificar afinidad ───────────────────────────────────────────────
        // AddPoints → suma (o resta si es negativo) al valor actual. Respeta globalMin/globalMax.
        // SetPoints → fija el valor exacto independientemente del valor anterior.
        // La relación debe existir en el mapa para que tenga efecto; si no existe no crea una nueva.

        Svc.AddPoints(player, npcA, 10);   // el jugador ayudó a npcA → +10
        Svc.SetPoints(player, npcB, 0);    // reset manual a neutral

        // Las relaciones son direccionales (A→B no implica B→A).
        // Para cambiar ambas direcciones a la vez hay que hacer dos llamadas.
        Svc.AddPoints(player, npcA, 5);
        Svc.AddPoints(npcA, player, 5);

        // ── Multi-personaje con LINQ ─────────────────────────────────────────
        // El servicio no tiene un listado global de personajes, por eso se pasa
        // la lista de candidatos manualmente y se filtra con LINQ.

        // ¿Quién del grupo tiene más afinidad hacia el jugador?
        var masAfin = grupo
            .Where(c => Svc.HasRelationship(c, player))       // solo los que tienen relación registrada
            .OrderByDescending(c => Svc.GetPoints(c, player)) // ordena de mayor a menor
            .FirstOrDefault();                                 // coge el primero, null si la lista está vacía

        Debug.Log($"Quien más aprecia al jugador: {masAfin?.displayName ?? "nadie"}");

        // Todos los miembros del grupo que son hostiles hacia el jugador (puntos negativos)
        var hostiles = grupo.Where(c => Svc.GetPoints(c, player) < 0).ToList();
        Debug.Log($"Hostiles en el grupo: {hostiles.Count}");

        // ¿Todo el grupo se lleva bien entre sí? Comprueba todas las direcciones posibles.
        bool grupoBueno = grupo.All(a =>
            grupo.Where(b => b != a).All(b => Svc.GetPoints(a, b) >= 1));

        Debug.Log($"Grupo unido: {grupoBueno}");

        // ── Eventos ──────────────────────────────────────────────────────────
        // OnAffinityChanged → se dispara cada vez que cambian los puntos entre dos personajes.
        // OnRelationshipChanged    → se dispara solo cuando el cambio cruza un umbral de banda.
        //                     Útil para mostrar notificaciones o desbloquear contenido.

        Svc.OnAffinityChanged += args => Debug.Log(
            $"Cambio de afinidad: {args.From.displayName}→{args.To.displayName} " +
            $"{args.OldPoints} → {args.NewPoints}");

        Svc.OnRelationshipChanged += args => Debug.Log(
            $"Cambio de nivel: {args.From.displayName}→{args.To.displayName} " +
            $"{args.OldRelationship?.name} → {args.NewRelationship?.name}");

        // ── Consultar tracks y bandas del schema ─────────────────────────────
        // El schema define los tracks disponibles y sus bandas (niveles).
        // Es solo datos de diseño — no cambia en runtime.

        // Lista todos los tracks configurados en el AffinitySchema
        foreach (var track in Svc.Schema.Tracks)
            Debug.Log($"Track '{track.id}' — {track.Relationships.Count} bandas");

        // Obtiene el track asignado al par player→npcA (sin hardcodear el ID)
        // GetTrack devuelve el ID del track que tiene este par en el CharacterAffinityMap.
        string trackIdPlayerA = Svc.GetTrack(player, npcA);
        var trackPlayerA = Svc.Schema.GetTrack(trackIdPlayerA);
        foreach (var banda in trackPlayerA.Relationships)
            Debug.Log($"  Banda '{banda.name}': puntos [{banda.minInclusive}, {banda.maxExclusive})  ordinal: {banda.ordinal}");

        // ── Condición por banda concreta (mínimo y máximo) ───────────────────
        // Caso de uso: "activa esta condición si la afinidad supera el mínimo de la banda 'Aliado'".
        // Se usa GetTrack para obtener el track real del par en lugar de hardcodear "social".
        // Esto es importante: distintos pares pueden usar tracks distintos.

        string trackId = Svc.GetTrack(player, npcA);
        var bandaObjetivo = Svc.Schema.GetTrack(trackId)?.Relationships
            .FirstOrDefault(b => b.name == "Aliado");

        if (bandaObjetivo != null)
        {
            int ptsActuales = Svc.GetPoints(player, npcA);

            // ¿Supera el mínimo? → la relación ha alcanzado o superado el umbral de esta banda
            if (ptsActuales >= bandaObjetivo.minInclusive)
                Debug.Log("Condición activa: superan el mínimo de Aliado.");

            // ¿Está por debajo del máximo? → la relación no ha sobrepasado el techo de esta banda
            // Nota: maxExclusive es exclusivo, el rango es [min, max)
            if (ptsActuales < bandaObjetivo.maxExclusive)
                Debug.Log("Condición activa: no superan el máximo de Aliado.");

            // ¿Está exactamente dentro del rango de la banda?
            // Equivalente a llamar a bandaObjetivo.Contains(ptsActuales)
            if (ptsActuales >= bandaObjetivo.minInclusive && ptsActuales < bandaObjetivo.maxExclusive)
                Debug.Log("Condición activa: están exactamente en banda Aliado.");
        }
    }
}
