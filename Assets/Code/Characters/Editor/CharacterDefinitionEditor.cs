#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterDefinition))]
public sealed class CharacterDefinitionEditor : Editor
{
    private const float IconSize  = 48f;
    private const float BarHeight = 8f;

    private CharacterAffinityMap _map;
    private SerializedObject     _mapSO;
    private bool                 _multipleMapWarning;

    private GUIStyle _rowStyle;
    private GUIStyle RowStyle => _rowStyle ??= new GUIStyle(EditorStyles.helpBox)
    {
        padding = new RectOffset(10, 10, 10, 10)
    };

    private GUIStyle _deleteButtonStyle;
    private GUIStyle DeleteButtonStyle => _deleteButtonStyle ??= new GUIStyle(EditorStyles.miniButton)
    {
        margin = new RectOffset(4, 0, 2, 2)
    };

    // Estado UI para añadir relaciones
    private CharacterDefinition _addOutTarget;
    private int                 _addOutPoints;
    private string              _addOutTrackId = "default";
    private bool                _showAddOut;

    private CharacterDefinition _addInTarget;
    private int                 _addInPoints;
    private string              _addInTrackId  = "default";
    private bool                _showAddIn;

    private void OnEnable()
    {
        var guids = AssetDatabase.FindAssets("t:CharacterAffinityMap");
        _multipleMapWarning = guids.Length > 1;
        if (guids.Length == 0) return;
        _map   = AssetDatabase.LoadAssetAtPath<CharacterAffinityMap>(AssetDatabase.GUIDToAssetPath(guids[0]));
        _mapSO = new SerializedObject(_map);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(12);

        if (_map == null)
        {
            EditorGUILayout.HelpBox("No se encontró ningún CharacterAffinityMap en el proyecto.", MessageType.Info);
            return;
        }

        if (_map.schema == null)
        {
            EditorGUILayout.HelpBox("El CharacterAffinityMap no tiene AffinitySchema asignado.", MessageType.Warning);
            return;
        }

        var character = (CharacterDefinition)target;

        DrawMapHeader();
        EditorGUILayout.Space(6);

        _mapSO.Update();

        int deleteIndex = DrawSection(character, outgoing: true);
        if (deleteIndex >= 0) { DeleteEntry(deleteIndex); return; }

        EditorGUILayout.Space(8);

        deleteIndex = DrawSection(character, outgoing: false);
        if (deleteIndex >= 0) { DeleteEntry(deleteIndex); return; }

        _mapSO.ApplyModifiedProperties();
    }

    // -----------------------------------------------------------------------
    // Cabecera del mapa
    // -----------------------------------------------------------------------

    private void DrawMapHeader()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField($"Mapa de afinidad:  {_map.name}", EditorStyles.miniLabel);
            if (GUILayout.Button("Abrir", EditorStyles.miniButton, GUILayout.Width(50)))
                Selection.activeObject = _map;
        }
        if (_multipleMapWarning)
            EditorGUILayout.HelpBox(
                "Hay más de un CharacterAffinityMap en el proyecto. Editando el primero encontrado.",
                MessageType.Warning);
    }

    // -----------------------------------------------------------------------
    // Sección (salientes o entrantes)
    // -----------------------------------------------------------------------

    private int DrawSection(CharacterDefinition character, bool outgoing)
    {
        string label = outgoing
            ? $"Relaciones de  {character.displayName}  →"
            : $"→  Relaciones hacia  {character.displayName}";

        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        var entries = _mapSO.FindProperty("entries");
        bool hasAny   = false;
        int  deleteAt = -1;

        for (int i = 0; i < entries.arraySize; i++)
        {
            var entry   = entries.GetArrayElementAtIndex(i);
            var fromObj = entry.FindPropertyRelative("from").objectReferenceValue as CharacterDefinition;
            var toObj   = entry.FindPropertyRelative("to").objectReferenceValue   as CharacterDefinition;

            bool matches = outgoing
                ? fromObj == character && toObj != null
                : toObj   == character && fromObj != null;

            if (!matches) continue;

            if (hasAny) EditorGUILayout.Space(4);
            hasAny = true;
            var other = outgoing ? toObj : fromObj;

            if (DrawEditableRow(entry, other))
                deleteAt = i;
        }

        if (!hasAny)
            EditorGUILayout.LabelField("  Sin relaciones registradas.", EditorStyles.miniLabel);

        EditorGUILayout.Space(4);
        DrawAddPanel(character, outgoing);

        return deleteAt;
    }

    // -----------------------------------------------------------------------
    // Fila editable
    // -----------------------------------------------------------------------

    /// <summary>
    /// Toda la fila se calcula con un unico Rect repartido a mano, sin scopes
    /// anidados de GUILayout. Se comprobo con Debug.Log que un VerticalScope
    /// con ExpandWidth, anidado dentro de un HorizontalScope que ya tiene un
    /// hijo de ancho fijo (el icono), calcula mal su ancho maximo y el
    /// contenido se sale de la fila. Un solo GetRect de nivel superior no
    /// tiene ese problema (medido: coincide siempre con el ancho visible).
    /// </summary>
    /// <returns>True si el usuario pulsó Eliminar.</returns>
    private bool DrawEditableRow(SerializedProperty entry, CharacterDefinition other)
    {
        var pointsProp  = entry.FindPropertyRelative("points");
        var trackIdProp = entry.FindPropertyRelative("trackId");
        int    points   = pointsProp.intValue;
        string trackId  = trackIdProp.stringValue;
        var    track    = _map.schema.GetTrack(trackId);
        int    tMin     = track?.MinPoints ?? 0;
        int    tMax     = track?.MaxPoints ?? 0;
        var    level    = _map.schema.GetRelationshipForPoints(points, trackId);

        bool multiTrack  = _map.schema.Tracks.Count > 1;
        bool wantsDelete = false;

        const float gap     = 6f;
        const float lineGap = 4f;
        const float barGap  = 5f;
        float lineH = EditorGUIUtility.singleLineHeight;

        float stackHeight = lineH + lineGap + BarHeight + barGap + lineH;
        if (multiTrack) stackHeight += barGap + lineH;

        float rowHeight = Mathf.Max(IconSize, stackHeight) + RowStyle.padding.vertical;

        Rect outer = GUILayoutUtility.GetRect(0, rowHeight, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
            RowStyle.Draw(outer, false, false, false, false);

        Rect content = RowStyle.padding.Remove(outer);

        Rect iconRect = new Rect(content.x, content.y, IconSize, IconSize);
        if (other.icon != null)
            GUI.DrawTexture(iconRect, other.icon.texture, ScaleMode.ScaleToFit);
        else
            EditorGUI.DrawRect(iconRect, new Color(0.22f, 0.22f, 0.22f));

        float x = iconRect.xMax + gap;
        float w = content.xMax - x;
        float y = content.y;

        // Cabecera: nombre, nivel actual, valor exacto (editable) y borrar.
        // El botón y el campo se anclan al borde derecho del hueco disponible.
        Rect deleteRect = new Rect(x + w - 22, y, 22, lineH);
        if (GUI.Button(deleteRect, "✕", DeleteButtonStyle))
            wantsDelete = true;

        Rect fieldRect = new Rect(deleteRect.x - 4 - 40, y, 40, lineH);
        EditorGUI.BeginChangeCheck();
        int fromField = EditorGUI.IntField(fieldRect, points);
        if (EditorGUI.EndChangeCheck())
            pointsProp.intValue = Mathf.Clamp(fromField, tMin, tMax);

        float nameWidth = EditorStyles.boldLabel.CalcSize(new GUIContent(other.displayName)).x;
        Rect nameRect = new Rect(x, y, Mathf.Min(nameWidth, w), lineH);
        EditorGUI.LabelField(nameRect, other.displayName, EditorStyles.boldLabel);

        if (level != null)
        {
            float levelWidth = EditorStyles.boldLabel.CalcSize(new GUIContent(level.name)).x;
            Rect levelRect = new Rect(nameRect.xMax + 8, y, levelWidth, lineH);
            if (levelRect.xMax < fieldRect.x)
                EditorGUI.LabelField(levelRect, level.name, EditorStyles.boldLabel);
        }

        y += lineH + lineGap;

        Rect barRect = new Rect(x, y, w, BarHeight);
        DrawBar(barRect, points, tMin, tMax, trackId);
        y += BarHeight + barGap;

        // Barra de arrastre pura, sin campo numerico incrustado (el numero
        // ya se muestra y edita arriba, en la cabecera).
        Rect sliderRect = new Rect(x, y, w, lineH);
        EditorGUI.BeginChangeCheck();
        int fromSlider = Mathf.RoundToInt(GUI.HorizontalSlider(sliderRect, points, tMin, tMax));
        if (EditorGUI.EndChangeCheck())
            pointsProp.intValue = Mathf.Clamp(fromSlider, tMin, tMax);
        y += lineH;

        if (multiTrack)
        {
            y += barGap;
            Rect trackRect = new Rect(x, y, w, lineH);
            DrawTrackPopup(trackRect, trackIdProp, trackId);
        }

        return wantsDelete;
    }

    // -----------------------------------------------------------------------
    // Panel para añadir nueva relación
    // -----------------------------------------------------------------------

    private void DrawAddPanel(CharacterDefinition character, bool outgoing)
    {
        ref bool                show     = ref outgoing ? ref _showAddOut : ref _showAddIn;
        ref CharacterDefinition addTarget= ref outgoing ? ref _addOutTarget : ref _addInTarget;
        ref int                 addPts   = ref outgoing ? ref _addOutPoints : ref _addInPoints;
        ref string              addTrack = ref outgoing ? ref _addOutTrackId : ref _addInTrackId;

        if (!show)
        {
            if (GUILayout.Button("+ Añadir relación", GUILayout.Height(22)))
                show = true;
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true)))
        {
            string pickLabel = outgoing ? "Personaje destino" : "Personaje origen";
            addTarget = (CharacterDefinition)EditorGUILayout.ObjectField(
                pickLabel, addTarget, typeof(CharacterDefinition), false);

            var addTrackObj = _map.schema.GetTrack(addTrack);
            int addMin = addTrackObj?.MinPoints ?? 0;
            int addMax = addTrackObj?.MaxPoints ?? 0;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Puntos iniciales", GUILayout.Width(110));

                EditorGUI.BeginChangeCheck();
                int fromSlider = Mathf.RoundToInt(GUILayout.HorizontalSlider(addPts, addMin, addMax, GUILayout.ExpandWidth(true)));
                if (EditorGUI.EndChangeCheck())
                    addPts = Mathf.Clamp(fromSlider, addMin, addMax);

                GUILayout.Space(4);

                EditorGUI.BeginChangeCheck();
                int fromField = EditorGUILayout.IntField(addPts, GUILayout.Width(40));
                if (EditorGUI.EndChangeCheck())
                    addPts = Mathf.Clamp(fromField, addMin, addMax);
            }

            var level = _map.schema.GetRelationshipForPoints(addPts, addTrack);
            if (level != null)
                EditorGUILayout.LabelField(level.name, EditorStyles.boldLabel);

            if (_map.schema.Tracks.Count > 1)
            {
                BuildTrackArrays(out var trackIds, out var trackNames);
                int currIdx = System.Array.IndexOf(trackIds, addTrack);
                if (currIdx < 0) currIdx = 0;
                int newIdx = EditorGUILayout.Popup("Track", currIdx, trackNames);
                addTrack = trackIds[newIdx];
            }

            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                bool canConfirm = addTarget != null && addTarget != character;
                using (new EditorGUI.DisabledScope(!canConfirm))
                {
                    if (GUILayout.Button("Confirmar"))
                    {
                        AddEntry(outgoing ? character : addTarget,
                                 outgoing ? addTarget  : character,
                                 addPts, addTrack);
                        addTarget = null;
                        addPts    = 0;
                        addTrack  = _map.schema.Tracks.FirstOrDefault()?.id ?? "default";
                        show      = false;
                    }
                }
                if (GUILayout.Button("Cancelar"))
                    show = false;
            }

            if (addTarget == character)
                EditorGUILayout.HelpBox("Un personaje no puede tener afinidad consigo mismo.", MessageType.Warning);
        }
    }

    // -----------------------------------------------------------------------
    // Mutaciones sobre el mapa
    // -----------------------------------------------------------------------

    private void AddEntry(CharacterDefinition from, CharacterDefinition to, int points, string trackId)
    {
        _mapSO.Update();
        var entries = _mapSO.FindProperty("entries");

        for (int i = 0; i < entries.arraySize; i++)
        {
            var e    = entries.GetArrayElementAtIndex(i);
            var fObj = e.FindPropertyRelative("from").objectReferenceValue;
            var tObj = e.FindPropertyRelative("to").objectReferenceValue;
            if (fObj == from && tObj == to) return;
        }

        entries.InsertArrayElementAtIndex(entries.arraySize);
        var newEntry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
        newEntry.FindPropertyRelative("from").objectReferenceValue = from;
        newEntry.FindPropertyRelative("to").objectReferenceValue   = to;
        newEntry.FindPropertyRelative("points").intValue           = points;
        newEntry.FindPropertyRelative("trackId").stringValue       = trackId ?? _map.schema.Tracks.FirstOrDefault()?.id ?? "default";

        _mapSO.ApplyModifiedProperties();
    }

    private void DeleteEntry(int index)
    {
        _mapSO.Update();
        _mapSO.FindProperty("entries").DeleteArrayElementAtIndex(index);
        _mapSO.ApplyModifiedProperties();
        GUIUtility.ExitGUI();
    }

    // -----------------------------------------------------------------------
    // Helpers de UI
    // -----------------------------------------------------------------------

    private void BuildTrackArrays(out string[] ids, out string[] names)
    {
        var tracks = _map.schema.Tracks;
        ids   = new string[tracks.Count];
        names = new string[tracks.Count];
        for (int i = 0; i < tracks.Count; i++)
        {
            ids[i]   = tracks[i].id;
            names[i] = tracks[i].displayName;
        }
    }

    private void DrawTrackPopup(Rect rect, SerializedProperty trackIdProp, string currentTrackId)
    {
        BuildTrackArrays(out var trackIds, out var trackNames);
        int currIdx = System.Array.IndexOf(trackIds, currentTrackId);
        if (currIdx < 0) currIdx = 0;

        EditorGUI.BeginChangeCheck();
        int newIdx = EditorGUI.Popup(rect, "Track", currIdx, trackNames);
        if (EditorGUI.EndChangeCheck())
            trackIdProp.stringValue = trackIds[newIdx];
    }

    private void DrawBar(Rect rect, int points, int min, int max, string trackId)
    {
        float range = max - min;
        if (range <= 0) return;
        float t     = Mathf.InverseLerp(min, max, points);
        Color color = _map.schema.GetColorForPoints(points, trackId);
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * t, rect.height), color);
    }
}
#endif
