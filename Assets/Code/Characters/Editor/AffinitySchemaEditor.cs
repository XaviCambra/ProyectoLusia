using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AffinitySchema))]
public sealed class AffinitySchemaEditor : Editor
{
    private SerializedProperty _tracks;

    private void OnEnable()
    {
        _tracks = serializedObject.FindProperty("tracks");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawTracks();

        serializedObject.ApplyModifiedProperties();
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
            t.FindPropertyRelative("relationships").ClearArray();
        }
    }

    /// <returns>True si el usuario pulsó eliminar el track.</returns>
    private bool DrawTrack(SerializedProperty trackProp, int trackIndex)
    {
        var idProp          = trackProp.FindPropertyRelative("id");
        var displayNameProp = trackProp.FindPropertyRelative("displayName");
        var relationshipsProp       = trackProp.FindPropertyRelative("relationships");

        bool wantsDelete = false;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(4);

            // Header
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField($"Track: {displayNameProp.stringValue}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("✕", EditorStyles.miniButton))
                    wantsDelete = true;
                GUILayout.Space(8);
            }

            EditorGUILayout.Space(6);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.PropertyField(idProp, new GUIContent("ID"));
                    EditorGUILayout.PropertyField(displayNameProp, new GUIContent("Nombre"));
                }
                GUILayout.Space(8);
            }

            EditorGUILayout.Space(6);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField("Niveles", EditorStyles.miniBoldLabel);
                GUILayout.Space(8);
            }

            EditorGUI.indentLevel++;

            int relationshipDeleteAt = -1;
            for (int i = 0; i < relationshipsProp.arraySize; i++)
            {
                EditorGUILayout.Space(6);
                if (DrawRelationship(relationshipsProp.GetArrayElementAtIndex(i), i))
                    relationshipDeleteAt = i;
            }

            if (relationshipDeleteAt >= 0)
            {
                relationshipsProp.DeleteArrayElementAtIndex(relationshipDeleteAt);
                EditorGUI.indentLevel--;
                return false;
            }

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                if (GUILayout.Button("+ Añadir nivel", GUILayout.Height(22)))
                {
                    relationshipsProp.InsertArrayElementAtIndex(relationshipsProp.arraySize);
                    var b = relationshipsProp.GetArrayElementAtIndex(relationshipsProp.arraySize - 1);
                    b.FindPropertyRelative("name").stringValue      = "Nuevo nivel";
                    b.FindPropertyRelative("ordinal").intValue      = relationshipsProp.arraySize - 1;
                    b.FindPropertyRelative("color").colorValue      = Color.gray;
                    b.FindPropertyRelative("minInclusive").intValue = 0;
                    b.FindPropertyRelative("maxExclusive").intValue = 10;
                }
                GUILayout.Space(8);
            }
            EditorGUILayout.Space(8);

            EditorGUI.indentLevel--;
        }

        return wantsDelete;
    }

    // -----------------------------------------------------------------------
    // Band (reutilizable por todos los tracks)
    // -----------------------------------------------------------------------

    /// <returns>True si el usuario pulsó eliminar.</returns>
    private bool DrawRelationship(SerializedProperty band, int index)
    {
        var nameProp    = band.FindPropertyRelative("name");
        var colorProp   = band.FindPropertyRelative("color");
        var ordinalProp = band.FindPropertyRelative("ordinal");
        var minProp     = band.FindPropertyRelative("minInclusive");
        var maxProp     = band.FindPropertyRelative("maxExclusive");
        var keyProp     = band.FindPropertyRelative("localizationKey");

        bool wantsDelete = false;

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(8); // margen lateral izquierdo de la caja del nivel

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                // Franja de color a la izquierda: se reserva con ExpandHeight y se dibuja
                // en Repaint, cuando el rect ya tiene la altura real del contenido de al lado.
                var colorRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20), GUILayout.ExpandHeight(true));
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(colorRect, colorProp.colorValue);

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.Space(4); // margen superior del contenido

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"#{ordinalProp.intValue}  {nameProp.stringValue}", EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(20)))
                            wantsDelete = true;
                    }

                    EditorGUI.indentLevel++;

                    // Ancho de label fijo y comun a todo el bloque para que los campos
                    // (Nombre, Clave, Ordinal, Rango) queden alineados en el mismo borde,
                    // y suficientemente ancho para que "Clave localización" no se corte.
                    float previousLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 145;

                    {
                        // Rects calculados a mano: el layout automatico de GUILayout dejaba
                        // un hueco grande entre el campo Nombre y su swatch de color.
                        const float colorSwatchWidth = 48f;
                        const float colorSwatchGap   = 4f;

                        Rect nameRow  = EditorGUILayout.GetControlRect();
                        Rect fieldRect = new Rect(nameRow.x, nameRow.y,
                            nameRow.width - colorSwatchWidth - colorSwatchGap, nameRow.height);
                        Rect swatchRect = new Rect(fieldRect.xMax + colorSwatchGap, nameRow.y,
                            colorSwatchWidth, nameRow.height);

                        nameProp.stringValue = EditorGUI.TextField(fieldRect, "Nombre", nameProp.stringValue);
                        colorProp.colorValue = EditorGUI.ColorField(swatchRect, GUIContent.none, colorProp.colorValue,
                            false, false, false);
                    }

                    EditorGUILayout.PropertyField(keyProp, new GUIContent("Clave localización"));

                    ordinalProp.intValue = EditorGUILayout.IntField("Ordinal", ordinalProp.intValue);
                    minProp.intValue     = EditorGUILayout.IntField("Rango minimo", minProp.intValue);
                    maxProp.intValue     = EditorGUILayout.IntField("Rango maximo", maxProp.intValue);

                    EditorGUIUtility.labelWidth = previousLabelWidth;

                    EditorGUI.indentLevel--;

                    EditorGUILayout.Space(4); // margen inferior del contenido
                }

                GUILayout.Space(2); // margen lateral derecho del contenido, dentro de la caja
            }

            GUILayout.Space(8); // margen lateral derecho de la caja del nivel
        }

        return wantsDelete;
    }
}