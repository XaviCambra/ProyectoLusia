#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AffinitySchema))]
public sealed class AffinitySchemaEditor : Editor
{
    private SerializedProperty _globalMin;
    private SerializedProperty _globalMax;
    private SerializedProperty _tracks;

    private void OnEnable()
    {
        _globalMin = serializedObject.FindProperty("globalMin");
        _globalMax = serializedObject.FindProperty("globalMax");
        _tracks    = serializedObject.FindProperty("tracks");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawGlobalSettings();
        EditorGUILayout.Space(10);
        DrawTracks();

        serializedObject.ApplyModifiedProperties();
    }

    // -----------------------------------------------------------------------
    // Configuración global
    // -----------------------------------------------------------------------

    private void DrawGlobalSettings()
    {
        EditorGUILayout.LabelField("Configuración global", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Rango de puntos", GUILayout.Width(120));
            _globalMin.intValue = EditorGUILayout.IntField(_globalMin.intValue, GUILayout.Width(60));
            EditorGUILayout.LabelField("→", GUILayout.Width(16));
            _globalMax.intValue = EditorGUILayout.IntField(_globalMax.intValue, GUILayout.Width(60));
        }

    }

    // -----------------------------------------------------------------------
    // Tracks
    // -----------------------------------------------------------------------

    private void DrawTracks()
    {
        EditorGUILayout.LabelField("Tracks de afinidad", EditorStyles.boldLabel);

        int deleteAt = -1;
        for (int i = 0; i < _tracks.arraySize; i++)
        {
            EditorGUILayout.Space(4);
            if (DrawTrack(_tracks.GetArrayElementAtIndex(i), i))
                deleteAt = i;
        }

        if (deleteAt >= 0)
        {
            _tracks.DeleteArrayElementAtIndex(deleteAt);
            serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
            return;
        }

        EditorGUILayout.Space(6);
        if (GUILayout.Button("+ Añadir track", GUILayout.Height(24)))
        {
            _tracks.InsertArrayElementAtIndex(_tracks.arraySize);
            var t = _tracks.GetArrayElementAtIndex(_tracks.arraySize - 1);
            t.FindPropertyRelative("id").stringValue          = $"track_{_tracks.arraySize}";
            t.FindPropertyRelative("displayName").stringValue = $"Track {_tracks.arraySize}";
            t.FindPropertyRelative("bands").ClearArray();
        }
    }

    /// <returns>True si el usuario pulsó eliminar el track.</returns>
    private bool DrawTrack(SerializedProperty trackProp, int trackIndex)
    {
        var idProp          = trackProp.FindPropertyRelative("id");
        var displayNameProp = trackProp.FindPropertyRelative("displayName");
        var bandsProp       = trackProp.FindPropertyRelative("bands");

        bool wantsDelete = false;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            // Header
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Track: {displayNameProp.stringValue}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("✕ Eliminar", EditorStyles.miniButton, GUILayout.Width(70)))
                    wantsDelete = true;
            }

            EditorGUI.indentLevel++;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(idProp,          new GUIContent("ID"));
                EditorGUILayout.PropertyField(displayNameProp, new GUIContent("Nombre"));
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Niveles", EditorStyles.miniBoldLabel);

            int bandDeleteAt = -1;
            for (int i = 0; i < bandsProp.arraySize; i++)
            {
                EditorGUILayout.Space(2);
                if (DrawBand(bandsProp.GetArrayElementAtIndex(i), i))
                    bandDeleteAt = i;
            }

            if (bandDeleteAt >= 0)
            {
                bandsProp.DeleteArrayElementAtIndex(bandDeleteAt);
                EditorGUI.indentLevel--;
                return false;
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("+ Añadir nivel", GUILayout.Height(22)))
            {
                bandsProp.InsertArrayElementAtIndex(bandsProp.arraySize);
                var b = bandsProp.GetArrayElementAtIndex(bandsProp.arraySize - 1);
                b.FindPropertyRelative("name").stringValue      = "Nuevo nivel";
                b.FindPropertyRelative("ordinal").intValue      = bandsProp.arraySize - 1;
                b.FindPropertyRelative("color").colorValue      = Color.gray;
                b.FindPropertyRelative("minInclusive").intValue = 0;
                b.FindPropertyRelative("maxExclusive").intValue = 10;
            }

            EditorGUI.indentLevel--;
        }

        return wantsDelete;
    }

    // -----------------------------------------------------------------------
    // Band (reutilizable por todos los tracks)
    // -----------------------------------------------------------------------

    /// <returns>True si el usuario pulsó eliminar.</returns>
    private bool DrawBand(SerializedProperty band, int index)
    {
        var nameProp    = band.FindPropertyRelative("name");
        var colorProp   = band.FindPropertyRelative("color");
        var ordinalProp = band.FindPropertyRelative("ordinal");
        var minProp     = band.FindPropertyRelative("minInclusive");
        var maxProp     = band.FindPropertyRelative("maxExclusive");
        var keyProp     = band.FindPropertyRelative("localizationKey");

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        using (new EditorGUILayout.HorizontalScope())
        {
            var colorRect = GUILayoutUtility.GetRect(12, 20, GUILayout.Width(12));
            EditorGUI.DrawRect(colorRect, colorProp.colorValue);
            EditorGUILayout.LabelField($"#{ordinalProp.intValue}  {nameProp.stringValue}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(20)))
            {
                EditorGUILayout.EndVertical();
                return true;
            }
        }

        EditorGUI.indentLevel++;

        using (new EditorGUILayout.HorizontalScope())
        {
            nameProp.stringValue = EditorGUILayout.TextField("Nombre", nameProp.stringValue);
            colorProp.colorValue = EditorGUILayout.ColorField(GUIContent.none, colorProp.colorValue,
                false, false, false, GUILayout.Width(48));
        }

        EditorGUILayout.PropertyField(keyProp,     new GUIContent("Clave localización"));
        EditorGUILayout.PropertyField(ordinalProp, new GUIContent("Ordinal"));

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Rango de puntos", GUILayout.Width(120));
            minProp.intValue = EditorGUILayout.IntField(minProp.intValue, GUILayout.Width(60));
            EditorGUILayout.LabelField("→", GUILayout.Width(16));
            maxProp.intValue = EditorGUILayout.IntField(maxProp.intValue, GUILayout.Width(60));
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
        return false;
    }
}
#endif
