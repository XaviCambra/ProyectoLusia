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

    [MenuItem("Window/Dialogue/Characters")]
    public static void Open() => GetWindow<CharacterProfilesWindow>("Characters");

    private void CreateGUI()
    {
        var root = rootVisualElement;
        root.style.paddingLeft   = MARGIN;
        root.style.paddingRight  = MARGIN;
        root.style.paddingTop    = MARGIN;
        root.style.paddingBottom = MARGIN;
        root.style.flexDirection = FlexDirection.Column;

        AddSpacer(root, 4);

        var createBtn = new Button(CreateNewCharacter) { text = "+ Nuevo Personaje..." };
        createBtn.style.alignSelf = Align.Stretch;
        root.Add(createBtn);
        AddSpacer(root, 4);

        var refreshBtn = new Button(Refresh) { text = "Actualizar" };
        refreshBtn.style.alignSelf = Align.Stretch;
        root.Add(refreshBtn);
        AddSpacer(root, 8);

        var scroll = new ScrollView { name = "list", style = { flexGrow = 1 } };
        root.Add(scroll);

        DrawList();
    }

    private void CreateNewCharacter()
    {
        var path = EditorUtility.SaveFilePanelInProject("Guardar Personaje", "NewCharacter", "asset", "");
        if (string.IsNullOrEmpty(path)) return;
        var p = CreateInstance<CharacterDefinition>();
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

        var characters = AssetDatabase
            .FindAssets("t:CharacterDefinition")
            .Select(guid => AssetDatabase.LoadAssetAtPath<CharacterDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(p => p != null)
            .OrderBy(p => p.displayName)
            .ToList();

        if (characters.Count == 0)
        {
            list.Add(new HelpBox("No hay CharacterDefinitions en el proyecto.", HelpBoxMessageType.Info));
            return;
        }

        var grid = new VisualElement
        {
            style =
            {
                flexDirection  = FlexDirection.Row,
                flexWrap       = Wrap.Wrap,
                justifyContent = Justify.FlexStart,
                alignItems     = Align.FlexStart,
            }
        };
        list.Add(grid);

        foreach (var c in characters)
            grid.Add(MakeCard(c));
    }

    private VisualElement MakeCard(CharacterDefinition c)
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

        // Thumbnail: primer retrato, o avatarSprite, o icono
        var firstSprite = c.portraits?.FirstOrDefault(e => e != null && e.sprite != null)?.sprite
                       ?? c.avatarSprite
                       ?? c.icon;

        var thumb = new VisualElement
        {
            style =
            {
                width  = THUMB, height = THUMB,
                alignSelf       = Align.Center,
                backgroundColor = new Color(0, 0, 0, firstSprite != null ? 0f : 0.08f),
                borderTopLeftRadius    = 8, borderTopRightRadius    = 8,
                borderBottomLeftRadius = 8, borderBottomRightRadius = 8,
            }
        };
        if (firstSprite != null)
        {
            thumb.Add(new Image { sprite = firstSprite, scaleMode = ScaleMode.ScaleToFit,
                style = { width = THUMB, height = THUMB } });
        }
        card.Add(thumb);
        AddSpacer(card, 8);

        var nameLbl = new Label(string.IsNullOrEmpty(c.displayName) ? "(Sin nombre)" : c.displayName)
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                unityTextAlign          = TextAnchor.MiddleCenter,
            }
        };
        card.Add(nameLbl);
        AddSpacer(card, 8);

        var openBtn = new Button(() => Selection.activeObject = c) { text = "Abrir" };
        card.Add(openBtn);

        return card;
    }

    private static void AddSpacer(VisualElement parent, float height)
        => parent.Add(new VisualElement { style = { height = height } });
}
#endif
