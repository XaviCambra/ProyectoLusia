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

    private float _rowContentHeight = IconSize;

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

    /// <returns>True si el usuario pulsó Eliminar.</returns>
    private bool DrawEditableRow(SerializedProperty entry, CharacterDefinition other)
    {
        var pointsProp  = entry.FindPropertyRelative("points");
        var trackIdProp = entry.FindPropertyRelative("trackId");
        int    points   = pointsProp.intValue;
        string trackId  = trackIdProp.stringValue;
        int    gMin     = _map.schema.globalMin;
        int    gMax     = _map.schema.globalMax;
        var    band     = _map.schema.GetBandForPoints(points, trackId);

        bool multiTrack  = _map.schema.Tracks.Count > 1;
        bool wantsDelete = false;

        using (new EditorGUILayout.HorizontalScope(RowStyle))
        {
            // Icono — ancho = alto cacheado del frame anterior → siempre cuadrado
            var iconRect = GUILayoutUtility.GetRect(
                _rowContentHeight, _rowContentHeight,
                GUILayout.Width(_rowContentHeight), GUILayout.ExpandHeight(true));

            if (Event.current.type == EventType.Repaint)
                _rowContentHeight = iconRect.height;

            if (other.icon != null)
                GUI.DrawTexture(iconRect, other.icon.texture, ScaleMode.ScaleToFit);
            else
                EditorGUI.DrawRect(iconRect, new Color(0.22f, 0.22f, 0.22f));

            GUILayout.Space(6);

            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(-1);
                    EditorGUILayout.LabelField(other.displayName, EditorStyles.boldLabel);

                    if (band != null)
                    {
                        var prev = GUI.contentColor;
                        GUI.contentColor = band.color;
                        EditorGUILayout.LabelField(band.name, EditorStyles.boldLabel, GUILayout.Width(110));
                        GUI.contentColor = prev;
                    }

                    if (multiTrack)
                    {
                        var track = _map.schema.GetTrack(trackId);
                        if (track != null)
                        {
                            var prev = GUI.contentColor;
                            GUI.contentColor = new Color(0.55f, 0.55f, 0.55f);
                            EditorGUILayout.LabelField($"[{track.displayName}]", EditorStyles.miniLabel, GUILayout.Width(80));
                            GUI.contentColor = prev;
                        }
                    }

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("✕", DeleteButtonStyle, GUILayout.Width(22)))
                        wantsDelete = true;
                }

                EditorGUILayout.Space(4);
                DrawBar(points, gMin, gMax, trackId);
                EditorGUILayout.Space(5);

                EditorGUI.BeginChangeCheck();
                Rect sliderRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
                int newVal = EditorGUI.IntSlider(sliderRect, points, gMin, gMax);
                if (EditorGUI.EndChangeCheck())
                    pointsProp.intValue = newVal;

                if (multiTrack)
                {
                    EditorGUILayout.Space(5);
                    DrawTrackPopup(trackIdProp, trackId);
                }
            }
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

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            string pickLabel = outgoing ? "Personaje destino" : "Personaje origen";
            addTarget = (CharacterDefinition)EditorGUILayout.ObjectField(
                pickLabel, addTarget, typeof(CharacterDefinition), false);

            addPts = EditorGUILayout.IntSlider("Puntos iniciales", addPts,
                _map.schema.globalMin, _map.schema.globalMax);

            var band = _map.schema.GetBandForPoints(addPts, addTrack);
            if (band != null)
            {
                var prev = GUI.contentColor;
                GUI.contentColor = band.color;
                EditorGUILayout.LabelField(band.name, EditorStyles.boldLabel);
                GUI.contentColor = prev;
            }

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

    private void DrawTrackPopup(SerializedProperty trackIdProp, string currentTrackId)
    {
        BuildTrackArrays(out var trackIds, out var trackNames);
        int currIdx = System.Array.IndexOf(trackIds, currentTrackId);
        if (currIdx < 0) currIdx = 0;
        EditorGUI.BeginChangeCheck();
        Rect popupRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
        popupRect.x     -= 1;
        popupRect.width += 1;
        int newIdx = EditorGUI.Popup(popupRect, "Track", currIdx, trackNames);
        if (EditorGUI.EndChangeCheck())
            trackIdProp.stringValue = trackIds[newIdx];
    }

    private void DrawBar(int points, int gMin, int gMax, string trackId)
    {
        float range = gMax - gMin;
        if (range <= 0) return;
        float t     = Mathf.InverseLerp(gMin, gMax, points);
        Color color = _map.schema.GetColorForPoints(points, trackId);
        Rect  bg    = GUILayoutUtility.GetRect(0, BarHeight, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(bg, new Color(0.15f, 0.15f, 0.15f));
        EditorGUI.DrawRect(new Rect(bg.x, bg.y, bg.width * t, bg.height), color);
    }
}
#endif
