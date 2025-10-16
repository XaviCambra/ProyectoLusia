#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Muestra cada PortraitEntry con thumbnail + campos
[CustomPropertyDrawer(typeof(PortraitEntry))]
public class PortraitEntryDrawer : PropertyDrawer
{
    private const float PADDING = 8f;
    private const float THUMB = 64f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Altura para una fila con miniatura grande
        return THUMB + PADDING * 2f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Quitar indentación del inspector
        position = EditorGUI.IndentedRect(position);

        // Fondo opcional
        var bg = new Rect(position.x, position.y, position.width, position.height);
        EditorGUI.DrawRect(bg, new Color(0, 0, 0, 0.04f));

        var keyProp = property.FindPropertyRelative("key");
        var spriteProp = property.FindPropertyRelative("sprite");

        // Zona miniatura
        var thumbRect = new Rect(position.x + PADDING, position.y + PADDING, THUMB, THUMB);

        // Zona de campos a la derecha
        var rightX = thumbRect.xMax + PADDING;
        var rightW = position.width - (rightX - position.x) - PADDING;
        var rowH = EditorGUIUtility.singleLineHeight;

        // 1) Dibuja miniatura
        Texture2D previewTex = null;
        var sprite = spriteProp.objectReferenceValue as Sprite;
        if (sprite != null)
        {
            // Intenta preview; si no, usa la textura base del sprite
            previewTex = AssetPreview.GetAssetPreview(sprite) ?? sprite.texture;
            if (previewTex != null)
                GUI.DrawTexture(thumbRect, previewTex, ScaleMode.ScaleToFit);
            else
                EditorGUI.HelpBox(thumbRect, "Sin preview", MessageType.None);
        }
        else
        {
            EditorGUI.HelpBox(thumbRect, "Sprite vacío", MessageType.Info);
        }

        // 2) Campo Key
        var keyLabel = new Rect(rightX, position.y + PADDING, 50f, rowH);
        var keyField = new Rect(keyLabel.xMax + 4f, position.y + PADDING, rightW - 54f, rowH);
        EditorGUI.LabelField(keyLabel, "Key");
        keyProp.stringValue = EditorGUI.TextField(keyField, keyProp.stringValue);

        // 3) Campo Sprite
        var spriteRect = new Rect(rightX, keyField.yMax + 6f, rightW, rowH);
        EditorGUI.PropertyField(spriteRect, spriteProp, GUIContent.none);

        EditorGUI.EndProperty();
    }
}
#endif
