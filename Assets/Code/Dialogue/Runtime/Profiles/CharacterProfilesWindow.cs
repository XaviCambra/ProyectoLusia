#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class CharacterProfilesWindow : EditorWindow
{
    private const float CARD_WIDTH   = 180f;
    private const float CARD_HEIGHT  = 240f;
    private const float CARD_MARGIN  = 6f;
    private const float THUMB        = 128f;
    private const float MARGIN       = 6f;

    [MenuItem("Window/Dialogue/Character Profiles")]
    public static void Open() => GetWindow<CharacterProfilesWindow>("Character Profiles");

    private void CreateGUI()
    {
        var root = rootVisualElement;
        root.style.paddingLeft   = MARGIN;
        root.style.paddingRight  = MARGIN;
        root.style.paddingTop    = MARGIN;
        root.style.paddingBottom = MARGIN;
        root.style.flexDirection = FlexDirection.Column;

        AddSpacer(root, 4);

        // --- Crear nuevo perfil ---
        var createBtn = new Button(CreateNewProfile) { text = "+ Nuevo Perfil..." };
        createBtn.style.alignSelf = Align.Stretch;
        root.Add(createBtn);
        AddSpacer(root, 4);

        // --- Actualizar ---
        var refreshBtn = new Button(Refresh) { text = "Actualizar" };
        refreshBtn.style.alignSelf = Align.Stretch;
        root.Add(refreshBtn);
        AddSpacer(root, 8);

        // --- Scroll lista ---
        var scroll = new ScrollView { name = "list", style = { flexGrow = 1 } };
        root.Add(scroll);

        DrawList();
    }

    private void CreateNewProfile()
    {
        var path = EditorUtility.SaveFilePanelInProject("Guardar Perfil", "NewCharacterProfile", "asset", "");
        if (string.IsNullOrEmpty(path)) return;
        var p = CreateInstance<CharacterProfile>();
        AssetDatabase.CreateAsset(p, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = p;
        DrawList();
    }

    private void Refresh()
    {
        AssetDatabase.Refresh();
        DrawList();
    }

    private void DrawList()
    {
        var list = rootVisualElement.Q<ScrollView>("list");
        if (list == null) return;
        list.Clear();

        var profiles = AssetDatabase
            .FindAssets("t:CharacterProfile")
            .Select(guid => AssetDatabase.LoadAssetAtPath<CharacterProfile>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(p => p != null)
            .OrderBy(p => p.displayName)
            .ToList();

        if (profiles.Count == 0)
        {
            list.Add(new HelpBox("No hay CharacterProfiles en el proyecto.", HelpBoxMessageType.Info));
            return;
        }

        var grid = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                flexWrap      = Wrap.Wrap,
                justifyContent = Justify.FlexStart,
                alignItems    = Align.FlexStart,
            }
        };
        list.Add(grid);

        foreach (var p in profiles)
            grid.Add(MakeCard(p));
    }

    private VisualElement MakeCard(CharacterProfile p)
    {
        var card = new Box
        {
            style =
            {
                width  = CARD_WIDTH,
                height = CARD_HEIGHT,
                marginLeft   = CARD_MARGIN, marginRight  = CARD_MARGIN,
                marginTop    = CARD_MARGIN, marginBottom = CARD_MARGIN,
                borderTopLeftRadius     = 4, borderTopRightRadius    = 4,
                borderBottomLeftRadius  = 4, borderBottomRightRadius = 4,
                paddingLeft  = 10, paddingRight  = 10,
                paddingTop   = 10, paddingBottom = 10,
                flexDirection  = FlexDirection.Column,
                justifyContent = Justify.Center,
                alignItems     = Align.Center,
            }
        };

        // Miniatura
        var firstSprite = p.portraits?.FirstOrDefault(e => e != null && e.sprite != null)?.sprite;
        var tex = GetSpritePreview(firstSprite);

        var thumb = new VisualElement
        {
            style =
            {
                width  = THUMB, height = THUMB,
                alignSelf = Align.Center,
                backgroundColor = new Color(0, 0, 0, tex != null ? 0f : 0.08f),
                borderTopLeftRadius    = 8, borderTopRightRadius    = 8,
                borderBottomLeftRadius = 8, borderBottomRightRadius = 8,
            }
        };
        if (tex != null)
        {
            thumb.Add(new Image { image = tex, scaleMode = ScaleMode.ScaleToFit,
                style = { width = THUMB, height = THUMB } });
        }
        card.Add(thumb);
        AddSpacer(card, 8);

        var nameLbl = new Label(string.IsNullOrEmpty(p.displayName) ? "(Sin nombre)" : p.displayName)
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                unityTextAlign          = TextAnchor.MiddleCenter,
            }
        };
        card.Add(nameLbl);
        AddSpacer(card, 8);

        var openBtn = new Button(() => Selection.activeObject = p) { text = "Abrir" };
        card.Add(openBtn);

        return card;
    }

    private static void AddSpacer(VisualElement parent, float height)
        => parent.Add(new VisualElement { style = { height = height } });

    private static Texture2D GetSpritePreview(Sprite sprite)
    {
        if (sprite == null) return null;
        return AssetPreview.GetAssetPreview(sprite) ?? sprite.texture;
    }
}
#endif
