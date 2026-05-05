#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AffinitySchema))]
public sealed class AffinitySchemaEditor : Editor
{
    private SerializedProperty _bands;
    private SerializedProperty _globalMin;
    private SerializedProperty _globalMax;
    private SerializedProperty _defaultPoints;

    private void OnEnable()
    {
        _bands         = serializedObject.FindProperty("bands");
        _globalMin     = serializedObject.FindProperty("globalMin");
        _globalMax     = serializedObject.FindProperty("globalMax");
        _defaultPoints = serializedObject.FindProperty("defaultPoints");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawGlobalSettings();
        EditorGUILayout.Space(8);
        DrawBands();

        serializedObject.ApplyModifiedProperties();
    }

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

        EditorGUILayout.PropertyField(_defaultPoints, new GUIContent("Puntos iniciales"));
    }

    private void DrawBands()
    {
        EditorGUILayout.LabelField("Niveles de afinidad", EditorStyles.boldLabel);

        for (int i = 0; i < _bands.arraySize; i++)
        {
            var band = _bands.GetArrayElementAtIndex(i);
            DrawBand(band, i);
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+ Añadir nivel", GUILayout.Height(24)))
            {
                _bands.InsertArrayElementAtIndex(_bands.arraySize);
                var newBand = _bands.GetArrayElementAtIndex(_bands.arraySize - 1);
                newBand.FindPropertyRelative("name").stringValue    = "Nuevo nivel";
                newBand.FindPropertyRelative("ordinal").intValue    = _bands.arraySize - 1;
                newBand.FindPropertyRelative("color").colorValue    = Color.gray;
                newBand.FindPropertyRelative("minInclusive").intValue = 0;
                newBand.FindPropertyRelative("maxExclusive").intValue = 10;
            }
        }
    }

    private void DrawBand(SerializedProperty band, int index)
    {
        var nameProp    = band.FindPropertyRelative("name");
        var colorProp   = band.FindPropertyRelative("color");
        var ordinalProp = band.FindPropertyRelative("ordinal");
        var minProp     = band.FindPropertyRelative("minInclusive");
        var maxProp     = band.FindPropertyRelative("maxExclusive");
        var keyProp     = band.FindPropertyRelative("localizationKey");

        Color bandColor = colorProp.colorValue;
        Rect  headerRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Barra de color + nombre
        using (new EditorGUILayout.HorizontalScope())
        {
            var colorRect = GUILayoutUtility.GetRect(12, 20, GUILayout.Width(12));
            EditorGUI.DrawRect(colorRect, bandColor);

            EditorGUILayout.LabelField($"#{ordinalProp.intValue}  {nameProp.stringValue}", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(20)))
            {
                _bands.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndVertical();
                return;
            }
        }

        EditorGUI.indentLevel++;

        // Nombre y color en la misma fila
        using (new EditorGUILayout.HorizontalScope())
        {
            nameProp.stringValue  = EditorGUILayout.TextField("Nombre", nameProp.stringValue);
            colorProp.colorValue  = EditorGUILayout.ColorField(GUIContent.none, colorProp.colorValue, false, false, false, GUILayout.Width(48));
        }

        EditorGUILayout.PropertyField(keyProp,     new GUIContent("Clave localización"));
        EditorGUILayout.PropertyField(ordinalProp, new GUIContent("Ordinal"));

        // Rango en una fila
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Rango de puntos", GUILayout.Width(120));
            minProp.intValue = EditorGUILayout.IntField(minProp.intValue, GUILayout.Width(60));
            EditorGUILayout.LabelField("→", GUILayout.Width(16));

            bool isUnbounded = maxProp.intValue == int.MaxValue;
            bool newUnbounded = EditorGUILayout.ToggleLeft("Sin tope", isUnbounded, GUILayout.Width(70));
            if (newUnbounded != isUnbounded)
                maxProp.intValue = newUnbounded ? int.MaxValue : minProp.intValue + 10;

            if (!newUnbounded)
                maxProp.intValue = EditorGUILayout.IntField(maxProp.intValue, GUILayout.Width(60));
            else
                EditorGUILayout.LabelField("∞", GUILayout.Width(20));
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
    }
}
#endif
