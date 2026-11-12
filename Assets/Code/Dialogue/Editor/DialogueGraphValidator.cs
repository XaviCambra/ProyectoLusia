// Editor/DialogueGraphValidator.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Validacion estatica de un <see cref="DialogueGraph"/>: detecta en el editor los
/// errores de autoria que en runtime fallan en silencio (modulos que nunca se
/// ejecutan, Choices que cuelgan el dialogo, aristas que se ignoran...).
/// No modifica el asset. Se usa desde el boton "Validar" de
/// <see cref="DialogueGraphEditorWindow"/> y desde el menu
/// Tools/Dialogue/Validar todos los grafos.
/// </summary>
public static class DialogueGraphValidator
{
    public enum Severity { Warning, Error }

    public readonly struct Issue
    {
        public readonly Severity severity;
        public readonly string   nodeGuid; // null si no es de un nodo concreto
        public readonly string   message;

        public Issue(Severity severity, string nodeGuid, string message)
        {
            this.severity = severity;
            this.nodeGuid = nodeGuid;
            this.message  = message;
        }
    }

    private const string NextPort = "Next";

    // -----------------------------------------------------------------------
    // Menu: validar todos los grafos del proyecto
    // -----------------------------------------------------------------------

    [MenuItem("Tools/Dialogue/Validar todos los grafos")]
    private static void ValidateAllGraphsMenu()
    {
        var chatModuleTypes = FindChatEngineModuleTypes();
        var chatGraphs      = FindChatGraphs();

        int graphs = 0, errors = 0, warnings = 0, dirty = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:DialogueGraph"))
        {
            var graph = AssetDatabase.LoadAssetAtPath<DialogueGraph>(AssetDatabase.GUIDToAssetPath(guid));
            if (graph == null) continue;
            graphs++;

            var engineTypes = chatGraphs.Contains(graph) ? chatModuleTypes : null;
            var issues = Validate(graph, engineTypes);
            if (issues.Count == 0) continue;

            dirty++;
            errors   += issues.Count(i => i.severity == Severity.Error);
            warnings += issues.Count(i => i.severity == Severity.Warning);
            Log(graph, issues);
        }

        var summary = $"[DialogueGraphValidator] {graphs} grafos revisados: {dirty} con problemas " +
                      $"({errors} errores, {warnings} avisos).";
        if (errors > 0) Debug.LogError(summary);
        else if (warnings > 0) Debug.LogWarning(summary);
        else Debug.Log(summary);
    }

    // -----------------------------------------------------------------------
    // API
    // -----------------------------------------------------------------------

    /// <summary>Valida un grafo deduciendo su motor: si alguna ChatConversation lo usa, se
    /// comprueba ademas que el runner de chat tenga executor para cada modulo.</summary>
    public static List<Issue> ValidateWithDetectedEngine(DialogueGraph graph)
    {
        var engineTypes = FindChatGraphs().Contains(graph) ? FindChatEngineModuleTypes() : null;
        return Validate(graph, engineTypes);
    }

    /// <summary>
    /// Valida el grafo. <paramref name="supportedModuleTypes"/> (opcional) son los tipos de
    /// modulo con executor en el runner que reproducira este grafo; si se pasa, se avisa de
    /// los modulos que ese runner se saltaria.
    /// </summary>
    public static List<Issue> Validate(DialogueGraph graph, ICollection<Type> supportedModuleTypes = null)
    {
        var issues = new List<Issue>();
        if (graph == null) return issues;

        var nodes = graph.Nodes ?? (IReadOnlyList<DialogueNodeData>)Array.Empty<DialogueNodeData>();
        var edges = graph.Edges ?? (IReadOnlyList<EdgeData>)Array.Empty<EdgeData>();

        // --- Nodos: GUIDs ---
        var byGuid = new Dictionary<string, DialogueNodeData>();
        foreach (var n in nodes)
        {
            if (n == null) continue;
            if (string.IsNullOrEmpty(n.GUID))
            {
                issues.Add(new Issue(Severity.Error, null, "Hay un nodo sin GUID: el runner lo ignora."));
                continue;
            }
            if (byGuid.ContainsKey(n.GUID))
            {
                issues.Add(new Issue(Severity.Error, n.GUID, $"{Label(n)}: GUID duplicado. El runner solo usa el primero."));
                continue;
            }
            byGuid.Add(n.GUID, n);
        }

        if (byGuid.Count == 0)
        {
            issues.Add(new Issue(Severity.Error, null, "El grafo no tiene nodos: el runner lanza una excepcion al iniciarlo."));
            return issues;
        }

        // --- Nodos: modulos y puertos esperados ---
        var expectedPorts = new Dictionary<string, HashSet<string>>(); // GUID -> puertos de salida validos
        foreach (var n in byGuid.Values)
            expectedPorts[n.GUID] = ValidateNode(n, supportedModuleTypes, issues);

        // --- Aristas ---
        var usedPorts = new Dictionary<(string, string), string>(); // (from, port) -> to
        var adjacency = new Dictionary<string, List<string>>();
        foreach (var e in edges)
        {
            if (e == null) continue;

            if (string.IsNullOrEmpty(e.fromNodeGUID) || !byGuid.TryGetValue(e.fromNodeGUID, out var from))
            {
                issues.Add(new Issue(Severity.Warning, null, $"Arista desde un nodo inexistente ('{e.fromNodeGUID}'). Se ignora."));
                continue;
            }
            if (string.IsNullOrEmpty(e.toNodeGUID) || !byGuid.ContainsKey(e.toNodeGUID))
            {
                issues.Add(new Issue(Severity.Error, from.GUID,
                    $"{Label(from)}: la arista del puerto '{e.fromPortName}' apunta a un nodo inexistente. En runtime ese camino termina el dialogo."));
                continue;
            }

            var port = string.IsNullOrEmpty(e.fromPortName) ? NextPort : e.fromPortName;
            var key  = (from.GUID, port);
            if (usedPorts.ContainsKey(key))
            {
                issues.Add(new Issue(Severity.Error, from.GUID,
                    $"{Label(from)}: varias aristas salen del puerto '{port}'. El runner solo usa la primera."));
                continue;
            }
            usedPorts[key] = e.toNodeGUID;

            if (!expectedPorts[from.GUID].Contains(port))
                issues.Add(new Issue(Severity.Warning, from.GUID,
                    $"{Label(from)}: arista desde el puerto '{port}', que el nodo ya no tiene (opcion borrada o renombrada). " +
                    "El runner podria usarla como fallback."));

            if (!adjacency.TryGetValue(from.GUID, out var list)) adjacency[from.GUID] = list = new List<string>();
            list.Add(e.toNodeGUID);
        }

        // --- Opciones sin arista ---
        foreach (var n in byGuid.Values)
        {
            if (!n.IsChoiceNode) continue;
            foreach (var port in expectedPorts[n.GUID])
                if (!usedPorts.ContainsKey((n.GUID, port)))
                    issues.Add(new Issue(Severity.Error, n.GUID,
                        $"{Label(n)}: la opcion con puerto '{port}' no esta conectada. Si el jugador la elige, " +
                        "el runner salta a otra arista cualquiera (o termina el dialogo)."));
        }

        // --- Nodo de inicio ---
        var starts = byGuid.Values.Where(n => n.isStart).ToList();
        if (starts.Count == 0)
        {
            issues.Add(new Issue(Severity.Warning, null,
                "Ningun nodo esta marcado como inicio: el runner elige uno por heuristica (el primero sin entradas)."));
        }
        else
        {
            foreach (var dup in starts.GroupBy(n => (n.startId ?? "").Trim().ToLowerInvariant()).Where(g => g.Count() > 1))
                issues.Add(new Issue(Severity.Warning, dup.First().GUID,
                    $"{dup.Count()} nodos de inicio comparten startId '{dup.Key}'. Sin override, el runner usa solo el primero."));
        }

        // --- Nodos inalcanzables ---
        var roots = starts.Count > 0
            ? starts.Select(n => n.GUID)
            : byGuid.Keys.Where(g => !usedPorts.Values.Contains(g));
        var reachable = new HashSet<string>();
        var stack = new Stack<string>(roots);
        while (stack.Count > 0)
        {
            var g = stack.Pop();
            if (!reachable.Add(g)) continue;
            if (adjacency.TryGetValue(g, out var next))
                foreach (var to in next) stack.Push(to);
        }
        foreach (var n in byGuid.Values)
            if (!reachable.Contains(n.GUID))
                issues.Add(new Issue(Severity.Warning, n.GUID, $"{Label(n)}: no se llega a este nodo desde ningun nodo de inicio."));

        return issues;
    }

    /// <summary>Valida los modulos de un nodo y devuelve los puertos de salida que deberia tener.</summary>
    private static HashSet<string> ValidateNode(DialogueNodeData n, ICollection<Type> supportedModuleTypes, List<Issue> issues)
    {
        var ports   = new HashSet<string>();
        var modules = n.modules ?? new List<IDialogueModule>();

        int choiceCount = 0;
        int firstChoiceIndex = -1;
        var missingExecutor = new HashSet<string>();

        for (int i = 0; i < modules.Count; i++)
        {
            var m = modules[i];
            if (m == null)
            {
                issues.Add(new Issue(Severity.Warning, n.GUID,
                    $"{Label(n)}: modulo #{i + 1} vacio (tipo borrado o renombrado). Se ignora."));
                continue;
            }

            if (supportedModuleTypes != null && !supportedModuleTypes.Contains(m.GetType()))
                missingExecutor.Add(m.DisplayName);

            if (m is ChoiceModule || m is ImageChoiceModule)
            {
                choiceCount++;
                if (firstChoiceIndex < 0) firstChoiceIndex = i;
            }
        }

        foreach (var name in missingExecutor)
            issues.Add(new Issue(Severity.Error, n.GUID,
                $"{Label(n)}: el runner de chat no tiene executor para '{name}'. Ese modulo se saltara en silencio" +
                (name == "Choices" || name == "Image Choice" ? " y el dialogo se quedara colgado en este nodo." : ".")));

        if (choiceCount > 1)
            issues.Add(new Issue(Severity.Error, n.GUID,
                $"{Label(n)}: tiene {choiceCount} modulos de eleccion. Solo se ejecuta el primero."));

        if (firstChoiceIndex >= 0)
        {
            int after = modules.Skip(firstChoiceIndex + 1).Count(x => x != null);
            if (after > 0)
                issues.Add(new Issue(Severity.Error, n.GUID,
                    $"{Label(n)}: hay {after} modulo(s) despues del Choice. Nunca se ejecutan: el Choice navega al elegir."));
        }

        // Los puertos los define el primer modulo de eleccion (igual que DialogueNodeView).
        var choice      = n.GetChoiceModule();
        var imageChoice = n.GetImageChoiceModule();

        if (choice != null)
        {
            var list = choice.choices ?? new List<ChoiceModule.ChoiceData>();
            if (list.Count == 0)
                issues.Add(new Issue(Severity.Error, n.GUID, $"{Label(n)}: Choice sin opciones. El dialogo se queda colgado."));

            for (int i = 0; i < list.Count; i++)
                AddChoicePort(n, list[i]?.portName, $"choice_{i}", ports, issues);

            var valid = list.Where(c => c != null).ToList();
            if (valid.Count > 0 && valid.All(c => c.requiresAffinity || c.requiresProgress))
                issues.Add(new Issue(Severity.Warning, n.GUID,
                    $"{Label(n)}: todas las opciones tienen condicion. Si no se cumple ninguna, el dialogo se para aqui. " +
                    "Anade una opcion sin condicion como salida segura."));
        }
        else if (imageChoice != null)
        {
            var list = imageChoice.choices ?? new List<ImageChoiceModule.ImageChoiceData>();
            if (list.Count == 0)
                issues.Add(new Issue(Severity.Error, n.GUID, $"{Label(n)}: Image Choice sin opciones. El dialogo se queda colgado."));

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].sprite == null)
                    issues.Add(new Issue(Severity.Warning, n.GUID, $"{Label(n)}: la opcion de imagen #{i + 1} no tiene sprite."));
                AddChoicePort(n, list[i]?.portName, $"img_{i}", ports, issues);
            }
        }
        else
        {
            ports.Add(NextPort);
        }

        return ports;
    }

    private static void AddChoicePort(DialogueNodeData n, string portName, string editorFallback,
                                      HashSet<string> ports, List<Issue> issues)
    {
        if (string.IsNullOrEmpty(portName))
        {
            // El editor dibuja el puerto como 'choice_i'/'img_i', pero el executor de chat
            // devuelve "" y el runner lo trata como "Next": la arista nunca se encuentra.
            issues.Add(new Issue(Severity.Error, n.GUID,
                $"{Label(n)}: una opcion no tiene portName (el editor la muestra como '{editorFallback}'). " +
                "En runtime no encontrara su arista."));
            ports.Add(editorFallback);
            return;
        }

        if (!ports.Add(portName))
            issues.Add(new Issue(Severity.Error, n.GUID,
                $"{Label(n)}: portName '{portName}' repetido en varias opciones. Solo una de ellas puede tener arista."));
    }

    // -----------------------------------------------------------------------
    // Motor de chat: que grafos usa y que modulos soporta
    // -----------------------------------------------------------------------

    /// <summary>Grafos referenciados por alguna ChatConversation (los reproduce el runner de chat).</summary>
    private static HashSet<DialogueGraph> FindChatGraphs()
    {
        var set = new HashSet<DialogueGraph>();
        foreach (var guid in AssetDatabase.FindAssets("t:ChatConversation"))
        {
            var conv = AssetDatabase.LoadAssetAtPath<ChatConversation>(AssetDatabase.GUIDToAssetPath(guid));
            if (conv != null && conv.graph != null) set.Add(conv.graph);
        }
        return set;
    }

    /// <summary>
    /// Tipos de modulo con executor en los prefabs de runner que usan las ChatPhoneApp del
    /// proyecto (campo 'runnerPrefab'). Null si no se encuentra ninguno (no se valida).
    /// </summary>
    private static HashSet<Type> FindChatEngineModuleTypes()
    {
        HashSet<Type> set = null;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (go == null) continue;

            foreach (var app in go.GetComponentsInChildren<ChatPhoneApp>(true))
            {
                var prop   = new SerializedObject(app).FindProperty("runnerPrefab");
                var runner = prop?.objectReferenceValue as DialogueRunner;
                if (runner == null) continue;

                set ??= new HashSet<Type>();
                foreach (var ex in runner.GetComponentsInChildren<IModuleExecutor>(true))
                    if (ex.ModuleType != null) set.Add(ex.ModuleType);
            }
        }
        return set;
    }

    // -----------------------------------------------------------------------
    // Utilidades
    // -----------------------------------------------------------------------

    public static void Log(DialogueGraph graph, IReadOnlyList<Issue> issues)
    {
        foreach (var i in issues)
        {
            var msg = $"[DialogueGraphValidator] '{graph.name}': {i.message}";
            if (i.severity == Severity.Error) Debug.LogError(msg, graph);
            else Debug.LogWarning(msg, graph);
        }
    }

    /// <summary>Identificador legible de un nodo para los mensajes: startId o inicio del primer texto, mas GUID corto.</summary>
    private static string Label(DialogueNodeData n)
    {
        var shortGuid = string.IsNullOrEmpty(n.GUID) ? "?" : n.GUID.Substring(0, Math.Min(8, n.GUID.Length));

        if (n.isStart && !string.IsNullOrEmpty(n.startId))
            return $"Nodo inicio '{n.startId}' ({shortGuid})";

        var text = n.modules?.OfType<TextModule>().FirstOrDefault(t => !string.IsNullOrEmpty(t.text))?.text;
        if (!string.IsNullOrEmpty(text))
        {
            text = text.Replace('\n', ' ');
            if (text.Length > 30) text = text.Substring(0, 30) + "...";
            return $"Nodo \"{text}\" ({shortGuid})";
        }

        return $"Nodo {shortGuid}";
    }
}
#endif
