// Editor/Profiles/CharacterProfilesWindow.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;

public class CharacterProfilesWindow : EditorWindow
{
    private CharacterProfileDatabase _db;

    // Ajustes del grid y las cards
    private const float CARD_WIDTH = 180f;
    private const float CARD_HEIGHT = 240f;
    private const float CARD_MARGIN = 6f;
    private const float THUMB = 128f;

    private ObjectField _addExistingField;

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
        root.style.flexDirection = FlexDirection.Column;
        root.style.flexGrow = 1;

        var header = new Label("Character Profiles Database");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        root.Add(header);
        AddSpacer(root, 4);

        // --- Campo Database ---
        var dbField = new ObjectField("Database") { objectType = typeof(CharacterProfileDatabase) };
        dbField.RegisterValueChangedCallback(e =>
        {
            _db = e.newValue as CharacterProfileDatabase;
            DrawList(root);
        });
        root.Add(dbField);
        AddSpacer(root, 4);

        // --- Botón crear nueva database ---
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
        { text = "Crear nueva Database..." };
        createDbBtn.style.height = 24;
        root.Add(createDbBtn);
        AddSpacer(root, 6);

        // --- "+ Añadir Perfil..." mismo estilo que crear database ---
        var addNewBtn = new Button(() =>
        {
            if (!EnsureDatabaseAssigned()) return;

            var p = ScriptableObject.CreateInstance<CharacterProfile>();
            p.name = "NewCharacterProfile";
            var path = EditorUtility.SaveFilePanelInProject("Guardar Perfil", p.name, "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(p, path);
                TryAddProfile(p);
            }
        })
        { text = "+ Añadir Perfil..." };
        addNewBtn.style.height = 24;
        addNewBtn.style.alignSelf = Align.Stretch;
        root.Add(addNewBtn);
        AddSpacer(root, 6);

        // --- Fila: ObjectField y "Añadir existente..." 50/50 ---
        var twoCol = new VisualElement { style = { flexDirection = FlexDirection.Row } };

        _addExistingField = new ObjectField() { objectType = typeof(CharacterProfile) };
        _addExistingField.tooltip = "Selecciona un CharacterProfile existente para añadirlo a la database.";
        _addExistingField.style.flexGrow = 1;
        _addExistingField.style.marginRight = 6;

        var addExistingBtn = new Button(() =>
        {
            if (!EnsureDatabaseAssigned()) return;
            var prof = _addExistingField.value as CharacterProfile;
            if (prof == null)
            {
                ShowNotification(new GUIContent("Selecciona un CharacterProfile."));
                return;
            }
            TryAddProfile(prof);
            _addExistingField.value = null;
        })
        { text = "Añadir existente..." };
        addExistingBtn.style.flexGrow = 1;
        addExistingBtn.style.height = 24;

        twoCol.Add(_addExistingField);
        twoCol.Add(addExistingBtn);
        root.Add(twoCol);
        AddSpacer(root, 6);

        // --- Botón Actualizar (ocupa todo el ancho, estilo igual) ---
        var refreshBtn = new Button(() => ForceRefresh()) { text = "Actualizar" };
        refreshBtn.style.height = 24;
        refreshBtn.style.alignSelf = Align.Stretch;
        root.Add(refreshBtn);
        AddSpacer(root, 8);

        // --- Zona Drag & Drop ---
        var dropZone = MakeDropZone();
        root.Add(dropZone);
        AddSpacer(root, 8);

        // --- Scroll de la lista ---
        var scroll = new ScrollView() { name = "list" };
        scroll.style.flexGrow = 1;
        root.Add(scroll);

        DrawList(root);
    }

    private bool EnsureDatabaseAssigned()
    {
        if (_db != null) return true;
        EditorUtility.DisplayDialog("Sin database", "Primero crea o asigna una CharacterProfileDatabase.", "OK");
        return false;
    }

    private void ForceRefresh()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (_db != null) _db.BuildIndex();
        DrawList(rootVisualElement);
        ShowNotification(new GUIContent("Actualizado"));
    }

    private void DrawList(VisualElement root)
    {
        var list = root.Q<ScrollView>("list");
        if (list == null)
        {
            list = new ScrollView() { name = "list" };
            list.style.flexGrow = 1;
            root.Add(list);
        }
        list.Clear();

        if (_db == null)
        {
            list.Add(new HelpBox("Selecciona o crea una Database.", HelpBoxMessageType.Info));
            return;
        }

        var grid = new VisualElement();
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.flexWrap = Wrap.Wrap;
        grid.style.justifyContent = Justify.FlexStart;
        grid.style.alignItems = Align.FlexStart;
        list.Add(grid);

        foreach (var p in _db.profiles.ToList())
        {
            if (p == null) continue;
            grid.Add(MakeProfileCard(p));
        }
    }

    private VisualElement MakeProfileCard(CharacterProfile p)
    {
        var card = new Box();
        card.style.width = CARD_WIDTH;
        card.style.height = CARD_HEIGHT;
        card.style.marginLeft = CARD_MARGIN;
        card.style.marginRight = CARD_MARGIN;
        card.style.marginTop = CARD_MARGIN;
        card.style.marginBottom = CARD_MARGIN;
        card.style.paddingLeft = 10;
        card.style.paddingRight = 10;
        card.style.paddingTop = 10;
        card.style.paddingBottom = 10;
        card.style.flexDirection = FlexDirection.Column;
        card.style.justifyContent = Justify.Center;
        card.style.alignItems = Align.Center;

        // Miniatura
        Texture2D tex = null;
        var firstSprite = p.portraits?.FirstOrDefault(e => e != null && e.sprite != null)?.sprite;
        tex = GetSpritePreview(firstSprite);

        var thumb = new VisualElement();
        thumb.style.width = THUMB;
        thumb.style.height = THUMB;
        thumb.style.alignSelf = Align.Center;
        thumb.style.backgroundColor = new Color(0, 0, 0, 0.08f);
        thumb.style.borderTopLeftRadius = 6;
        thumb.style.borderTopRightRadius = 6;
        thumb.style.borderBottomLeftRadius = 6;
        thumb.style.borderBottomRightRadius = 6;

        if (tex != null)
        {
            var img = new Image();
            img.style.width = THUMB;
            img.style.height = THUMB;
            img.scaleMode = ScaleMode.ScaleToFit;
            img.image = tex;
            thumb.style.backgroundColor = new Color(0, 0, 0, 0f);
            thumb.Add(img);
        }

        card.Add(thumb);
        AddSpacer(card, 8);

        var nameLbl = new Label(string.IsNullOrEmpty(p.displayName) ? "(Sin nombre)" : p.displayName);
        nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
        card.Add(nameLbl);

        if (!string.IsNullOrEmpty(p.pseudonym))
        {
            var pseudoLbl = new Label(p.pseudonym);
            pseudoLbl.style.opacity = 0.75f;
            pseudoLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            card.Add(pseudoLbl);
        }
        AddSpacer(card, 8);

        var btnRow = new VisualElement();
        btnRow.style.flexDirection = FlexDirection.Row;
        btnRow.style.justifyContent = Justify.Center;

        var openBtn = new Button(() => Selection.activeObject = p) { text = "Abrir" };
        var removeBtn = new Button(() =>
        {
            if (EditorUtility.DisplayDialog("Quitar de DB",
                $"¿Quitar '{p.name}' de la database?\n(NO borra el asset)", "Sí", "No"))
            {
                _db.profiles.Remove(p);
                EditorUtility.SetDirty(_db);
                _db.BuildIndex();
                DrawList(rootVisualElement);
            }
        })
        { text = "Quitar" };

        openBtn.style.marginRight = 4;
        btnRow.Add(openBtn);
        btnRow.Add(removeBtn);
        card.Add(btnRow);

        return card;
    }

    // Drag & Drop zone
    private VisualElement MakeDropZone()
    {
        var dz = new VisualElement();
        dz.style.height = 36;
        dz.style.backgroundColor = new Color(0, 0, 0, 0.08f);
        dz.style.borderTopLeftRadius = 6;
        dz.style.borderTopRightRadius = 6;
        dz.style.borderBottomLeftRadius = 6;
        dz.style.borderBottomRightRadius = 6;
        dz.style.alignItems = Align.Center;
        dz.style.justifyContent = Justify.Center;

        var lbl = new Label("Arrastra aquí CharacterProfile para añadirlos a la Database");
        lbl.style.opacity = 0.8f;
        dz.Add(lbl);

        dz.RegisterCallback<DragUpdatedEvent>(evt =>
        {
            if (HasDraggedProfiles())
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopImmediatePropagation();
            }
        });

        dz.RegisterCallback<DragPerformEvent>(evt =>
        {
            if (!HasDraggedProfiles()) return;
            DragAndDrop.AcceptDrag();
            int added = 0;
            foreach (var o in DragAndDrop.objectReferences)
                if (o is CharacterProfile prof && TryAddProfile(prof)) added++;
            ShowNotification(new GUIContent($"Añadidos {added} perfil(es)"));
            evt.StopImmediatePropagation();
        });

        return dz;
    }

    private bool HasDraggedProfiles()
    {
        if (_db == null) return false;
        return DragAndDrop.objectReferences.Any(o => o is CharacterProfile);
    }

    private bool TryAddProfile(CharacterProfile p)
    {
        if (_db == null || p == null) return false;
        if (_db.profiles == null) _db.profiles = new List<CharacterProfile>();
        if (_db.profiles.Contains(p))
        {
            ShowNotification(new GUIContent("Ese perfil ya está en la database."));
            return false;
        }
        _db.profiles.Add(p);
        EditorUtility.SetDirty(_db);
        _db.BuildIndex();
        DrawList(rootVisualElement);
        return true;
    }

    // Helpers
    private static void AddSpacer(VisualElement parent, float height)
    {
        var sp = new VisualElement { style = { height = height } };
        parent.Add(sp);
    }

    private static Texture2D GetSpritePreview(Sprite sprite)
    {
        if (sprite == null) return null;
        var tex = AssetPreview.GetAssetPreview(sprite);
        return tex ?? sprite.texture;
    }
}
#endif
