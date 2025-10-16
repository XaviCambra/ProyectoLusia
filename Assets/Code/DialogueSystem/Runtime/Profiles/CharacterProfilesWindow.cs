// Editor/Profiles/CharacterProfilesWindow.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class CharacterProfilesWindow : EditorWindow
{
    private CharacterProfileDatabase _db;

    [MenuItem("Window/Dialogue/Character Profiles")]
    public static void Open()
    {
        GetWindow<CharacterProfilesWindow>("Character Profiles");
    }

    private void CreateGUI()
    {
        var root = rootVisualElement;
        root.style.paddingLeft = 8;
        root.style.paddingRight = 8;
        root.style.paddingTop = 8;

        var header = new Label("Character Profiles Database");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        root.Add(header);

        var dbField = new ObjectField("Database") { objectType = typeof(CharacterProfileDatabase) };
        dbField.RegisterValueChangedCallback(e =>
        {
            _db = e.newValue as CharacterProfileDatabase;
            DrawList(root);
        });
        root.Add(dbField);

        var createDbBtn = new Button(() =>
        {
            var path = EditorUtility.SaveFilePanelInProject("Crear Database", "CharacterProfiles", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                var db = ScriptableObject.CreateInstance<CharacterProfileDatabase>();
                AssetDatabase.CreateAsset(db, path);
                AssetDatabase.SaveAssets();
                _db = db;
                dbField.value = db;
                DrawList(root);
            }
        })
        { text = "Crear nueva Database…" };
        root.Add(createDbBtn);

        DrawList(root);
    }

    private void DrawList(VisualElement root)
    {
        root.Q<ScrollView>("list")?.RemoveFromHierarchy();
        var list = new ScrollView() { name = "list" };
        root.Add(list);

        if (_db == null)
        {
            list.Add(new HelpBox("Selecciona o crea una Database.", HelpBoxMessageType.Info));
            return;
        }

        // Botón añadir perfil
        var addRow = new VisualElement();
        addRow.style.flexDirection = FlexDirection.Row;
        var btnAdd = new Button(() =>
        {
            var p = ScriptableObject.CreateInstance<CharacterProfile>();
            p.name = "NewCharacterProfile";
            var path = EditorUtility.SaveFilePanelInProject("Guardar Perfil", p.name, "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(p, path);
                _db.profiles.Add(p);
                EditorUtility.SetDirty(_db);
                _db.BuildIndex();
                DrawList(root);
            }
        })
        { text = "+ Añadir Perfil…" };
        addRow.Add(btnAdd);
        list.Add(addRow);

        // Lista de perfiles
        foreach (var p in _db.profiles.ToList())
        {
            if (p == null) continue;
            var box = new Box();
            box.style.marginTop = 6;
            box.style.paddingLeft = 6;
            box.style.paddingRight = 6;

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var pf = new ObjectField() { objectType = typeof(CharacterProfile), value = p, label = "Perfil" };
            pf.RegisterValueChangedCallback(e =>
            {
                var idx = _db.profiles.IndexOf(p);
                _db.profiles[idx] = e.newValue as CharacterProfile;
                EditorUtility.SetDirty(_db);
                _db.BuildIndex();
            });

            var openBtn = new Button(() => Selection.activeObject = p) { text = "Abrir" };
            var removeBtn = new Button(() =>
            {
                if (EditorUtility.DisplayDialog("Quitar de DB", $"¿Quitar '{p.name}' de la database?\n(NO borra el asset)", "Sí", "No"))
                {
                    _db.profiles.Remove(p);
                    EditorUtility.SetDirty(_db);
                    _db.BuildIndex();
                    DrawList(root);
                }
            })
            { text = "Quitar" };

            row.Add(pf);
            row.Add(openBtn);
            row.Add(removeBtn);
            box.Add(row);

            var idLbl = new Label($"ID: {p.ProfileId}");
            idLbl.style.opacity = 0.7f;
            box.Add(idLbl);

            list.Add(box);
        }
    }
}
#endif
