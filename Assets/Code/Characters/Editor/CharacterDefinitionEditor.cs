#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterDefinition))]
public sealed class CharacterDefinitionEditor : Editor
{
    private const float BarHeight = 8f;

    // Recuerda que CharacterAffinityMap eligio el usuario la ultima vez, para no
    // depender de "el primero que encuentre AssetDatabase" (orden no fiable,
    // puede cambiar entre sesiones). Ver DrawMapHeader.
    private const string SelectedMapPrefKey = "ProyectoLusia.CharacterDefinitionEditor.SelectedAffinityMapGuid";

    private string[]             _allMapGuids;
    private string               _selectedMapGuid;
    private CharacterAffinityMap _map;
    private SerializedObject     _mapSO;

    private GUIStyle _rowStyle;
    private GUIStyle RowStyle => _rowStyle ??= new GUIStyle(EditorStyles.helpBox)
    {
        padding = new RectOffset(9, 9, 10, 10)
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
        _allMapGuids = AssetDatabase.FindAssets("t:CharacterAffinityMap");

        // Recupera la eleccion anterior si sigue existiendo; si no, cae al
        // primero encontrado (unico caso donde "el primero" es aceptable: no
        // hay eleccion previa que respetar todavia).
        _selectedMapGuid = EditorPrefs.GetString(SelectedMapPrefKey, "");
        if (string.IsNullOrEmpty(_selectedMapGuid) || System.Array.IndexOf(_allMapGuids, _selectedMapGuid) < 0)
            _selectedMapGuid = _allMapGuids.Length > 0 ? _allMapGuids[0] : null;

        LoadSelectedMap();
    }

    private void LoadSelectedMap()
    {
        if (string.IsNullOrEmpty(_selectedMapGuid)) { _map = null; _mapSO = null; return; }
        _map   = AssetDatabase.LoadAssetAtPath<CharacterAffinityMap>(AssetDatabase.GUIDToAssetPath(_selectedMapGuid));
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

        if (_allMapGuids.Length <= 1) return;

        // El sistema en runtime solo lee UN CharacterAffinityMap (el que tenga
        // asignado AffinityServiceBootstrapper en la escena) — si hay varios en
        // el proyecto, dejamos elegir cual editar en vez de adivinar "el
        // primero que encuentre AssetDatabase" (orden no fiable). La eleccion
        // se recuerda entre sesiones (EditorPrefs), no es un dato del proyecto.
        EditorGUILayout.HelpBox(
            $"Hay {_allMapGuids.Length} CharacterAffinityMap en el proyecto, pero solo se usa uno en runtime " +
            "(el asignado en AffinityServiceBootstrapper). Elige cual editar para no perder cambios en el que no cuenta.",
            MessageType.Warning);

        var names = _allMapGuids
            .Select(guid => System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)))
            .ToArray();
        int currentIndex = System.Array.IndexOf(_allMapGuids, _selectedMapGuid);

        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUILayout.Popup("Editando:", currentIndex, names);
        if (EditorGUI.EndChangeCheck() && newIndex != currentIndex)
        {
            _selectedMapGuid = _allMapGuids[newIndex];
            EditorPrefs.SetString(SelectedMapPrefKey, _selectedMapGuid);
            LoadSelectedMap();
            GUIUtility.ExitGUI(); // el layout de este frame ya no coincide con el mapa nuevo
        }
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
    /// <returns>True si el usuario pulso Eliminar.</returns>
    private bool DrawEditableRow(SerializedProperty entry, CharacterDefinition other)
    {
        // --- Datos de la relacion: puntos, track asignado, nivel actual y si ya se conoce ---
        var pointsProp  = entry.FindPropertyRelative("points");
        var trackIdProp = entry.FindPropertyRelative("trackId");
        var knownProp   = entry.FindPropertyRelative("known");
        int    points   = pointsProp.intValue;
        string trackId  = trackIdProp.stringValue;
        var    track    = _map.schema.GetTrack(trackId);
        int    tMin     = track?.MinPoints ?? 0;
        int    tMax     = track?.MaxPoints ?? 0;
        var    level    = _map.schema.GetRelationshipForPoints(points, trackId);

        bool multiTrack  = _map.schema.Tracks.Count > 1;
        bool wantsDelete = false;
        Sprite icon = other.icon;

        // --- Medidas base de la fila ---
        const float gap        = 6f; // hueco entre el icono y el contenido
        const float lineGap    = 4f; // hueco vertical cabecera -> barra
        const float barGap     = 5f; // hueco vertical entre barra/slider/track
        const float namePadding = 3f; // espacio entre nombre y nivel actual
        float lineH = EditorGUIUtility.singleLineHeight;

        // Alto del bloque de contenido (cabecera + barra + slider + track si aplica).
        // Este valor manda: el icono ocupa exactamente este alto (ver mas abajo),
        // no al reves. La fila crece o encoge segun cuanto contenido tenga.
        float stackHeight = lineH + lineGap + BarHeight + barGap + lineH;
        if (multiTrack) stackHeight += barGap + lineH;
        stackHeight += barGap + lineH; // fila del toggle "Conocida"

        float rowHeight = stackHeight + RowStyle.padding.vertical;

        // --- Caja de fondo de la fila ---
        // Toda la fila usa UN SOLO GetRect de nivel superior (fiable, comprobado
        // con Debug.Log) y reparte el resto a mano con Rects. No se usan scopes
        // anidados de GUILayout: un VerticalScope(ExpandWidth) anidado dentro de
        // un HorizontalScope con un hijo de ancho fijo (el icono) calculaba mal
        // su ancho maximo y el contenido se salia de la fila.
        Rect outer = GUILayoutUtility.GetRect(0, rowHeight, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
            RowStyle.Draw(outer, false, false, false, false);

        Rect content = RowStyle.padding.Remove(outer); // area interior, ya sin el padding de la caja

        // --- Icono del personaje: cuadrado, lado = alto del contenido (stackHeight) ---
        // asi ocupa siempre el maximo alto disponible y el ancho se ajusta igual (1:1).
        Rect iconRect = new Rect(content.x, content.y, stackHeight, stackHeight);
        if (icon != null)
            DrawSpriteContained(iconRect, icon);
        else
            EditorGUI.DrawRect(iconRect, new Color(0.22f, 0.22f, 0.22f));

        // x/w = columna de contenido a la derecha del icono; y = cursor vertical,
        // se va desplazando hacia abajo segun se van dibujando las filas internas.
        float x = iconRect.xMax + gap;
        float w = content.xMax - x;
        float y = content.y;

        // --- Cabecera: nombre, nivel actual, valor exacto (editable) y borrar ---
        // El boton de borrar y el campo numerico se anclan al borde derecho del
        // hueco disponible; nombre y nivel se dibujan pegados a la izquierda.
        Rect deleteRect = new Rect(x + w - 22, y, 22, lineH);
        if (GUI.Button(deleteRect, "✕", DeleteButtonStyle))
            wantsDelete = true;

        Rect fieldRect = new Rect(deleteRect.x - 4 - 40, y, 40, lineH);
        EditorGUI.BeginChangeCheck();
        int fromField = EditorGUI.IntField(fieldRect, points);
        if (EditorGUI.EndChangeCheck())
            pointsProp.intValue = Mathf.Clamp(fromField, tMin, tMax);

        // Nombre del personaje con el que hay relacion.
        float nameWidth = EditorStyles.boldLabel.CalcSize(new GUIContent(other.displayName)).x;
        Rect nameRect = new Rect(x, y, Mathf.Min(nameWidth, w), lineH);
        EditorGUI.LabelField(nameRect, other.displayName, EditorStyles.boldLabel);

        // Nombre del nivel de afinidad actual (ej. "Amistad"), centrado sobre la
        // barra de progreso de abajo. Si no cabe centrado (nombre largo o nivel
        // ancho), se desplaza lo justo para no solaparse con nombre ni campo.
        if (level != null)
        {
            float levelWidth = EditorStyles.boldLabel.CalcSize(new GUIContent(level.name)).x;
            float centeredX  = x + (w - levelWidth) / 2f;
            centeredX = Mathf.Max(centeredX, nameRect.xMax + namePadding);
            centeredX = Mathf.Min(centeredX, fieldRect.x - namePadding - levelWidth);

            Rect levelRect = new Rect(centeredX, y, levelWidth, lineH);
            if (levelRect.xMax < fieldRect.x && levelRect.x >= nameRect.xMax)
                EditorGUI.LabelField(levelRect, level.name, EditorStyles.boldLabel);
        }

        y += lineH + lineGap;

        // --- Barra de progreso: color e indicador visual del nivel actual ---
        Rect barRect = new Rect(x, y, w, BarHeight);
        DrawBar(barRect, points, tMin, tMax, trackId);
        y += BarHeight + barGap;

        // --- Slider de arrastre para cambiar los puntos ---
        // Barra pura, sin campo numerico incrustado: el numero ya se muestra y
        // edita arriba, en la cabecera (fieldRect).
        Rect sliderRect = new Rect(x, y, w, lineH);
        EditorGUI.BeginChangeCheck();
        int fromSlider = Mathf.RoundToInt(GUI.HorizontalSlider(sliderRect, points, tMin, tMax));
        if (EditorGUI.EndChangeCheck())
            pointsProp.intValue = Mathf.Clamp(fromSlider, tMin, tMax);
        y += lineH;

        // --- Selector de track (solo si hay mas de un track definido) ---
        if (multiTrack)
        {
            y += barGap;
            Rect trackRect = new Rect(x, y, w, lineH);
            DrawTrackPopup(trackRect, trackIdProp, trackId);
            y += lineH;
        }

        // --- Toggle "Conocida": el dato de afinidad puede existir precableado desde
        // el principio sin que eso signifique que el personaje origen ya se topo con
        // el otro. Por defecto false; se activa por historia via IAffinityService.SetKnown,
        // pero tambien se puede marcar/desmarcar a mano aqui para pruebas.
        y += barGap;
        Rect knownRect = new Rect(x, y, w, lineH);
        var fromName = (entry.FindPropertyRelative("from").objectReferenceValue as CharacterDefinition)?.displayName ?? "?";
        var knownContent = new GUIContent("Conocida", $"Si esta marcado, {fromName} ya conoce/detecta esta relacion (IAffinityService.IsKnown). Desmarcado por defecto aunque el dato ya tenga puntos.");
        EditorGUI.BeginChangeCheck();
        bool newKnown = EditorGUI.ToggleLeft(knownRect, knownContent, knownProp.boolValue);
        if (EditorGUI.EndChangeCheck())
            knownProp.boolValue = newKnown;

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

            // Barra de arrastre + campo numerico por separado (igual que en la fila
            // de arriba): asi el numero siempre tiene su ancho fijo garantizado.
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
                int newIdx = EditorGUILayout.Popup("Track:", currIdx, trackNames);
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
        newEntry.FindPropertyRelative("known").boolValue            = false;

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

    /// <summary>Desplegable para elegir a que track (linea de afinidad) sigue una relacion.</summary>
    private void DrawTrackPopup(Rect rect, SerializedProperty trackIdProp, string currentTrackId)
    {
        BuildTrackArrays(out var trackIds, out var trackNames);
        int currIdx = System.Array.IndexOf(trackIds, currentTrackId);
        if (currIdx < 0) currIdx = 0;

        EditorGUI.BeginChangeCheck();
        int newIdx = EditorGUI.Popup(rect, "Track:", currIdx, trackNames);
        if (EditorGUI.EndChangeCheck())
            trackIdProp.stringValue = trackIds[newIdx];
    }

    /// <summary>
    /// Dibuja solo el recorte del sprite dentro de su textura (coordenadas UV
    /// normalizadas, no la textura completa, que puede ser un atlas con relleno).
    /// Se ajusta al recuadro sin recortar ni distorsionar (mismo aspecto que el
    /// sprite original) y se centra dentro de el.
    /// </summary>
    private void DrawSpriteContained(Rect box, Sprite sprite)
    {
        Rect uv = new Rect(
            sprite.rect.x      / sprite.texture.width,
            sprite.rect.y      / sprite.texture.height,
            sprite.rect.width  / sprite.texture.width,
            sprite.rect.height / sprite.texture.height);

        float fitScale = Mathf.Min(box.width / sprite.rect.width, box.height / sprite.rect.height);
        float drawW = sprite.rect.width  * fitScale;
        float drawH = sprite.rect.height * fitScale;
        Rect drawRect = new Rect(
            box.x + (box.width  - drawW) / 2f,
            box.y + (box.height - drawH) / 2f,
            drawW, drawH);

        GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, uv);
    }

    /// <summary>
    /// Barra de progreso: fondo oscuro + relleno coloreado segun el porcentaje
    /// que representan los puntos actuales dentro del rango [min, max) del track.
    /// El color viene del nivel de afinidad correspondiente (ver AffinitySchema).
    /// </summary>
    private void DrawBar(Rect rect, int points, int min, int max, string trackId)
    {
        float range = max - min;
        if (range <= 0) return; // rango invalido (track sin niveles definidos)

        float t     = Mathf.InverseLerp(min, max, points);
        Color color = _map.schema.GetColorForPoints(points, trackId);

        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f)); // fondo
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * t, rect.height), color); // relleno
    }
}
#endif
