// Editor/GraphView/DialogueGraphEditorWindow.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphEditorWindow : EditorWindow
{
    private DialogueGraphView _graphView;
    private DialogueGraph _asset;

    [MenuItem("Window/Dialogue/Dialogue Graph Editor")]
    public static void Open()
    {
        var wnd = GetWindow<DialogueGraphEditorWindow>();
        wnd.titleContent = new GUIContent("Dialogue Graph");
        wnd.minSize = new Vector2(600, 400);
    }

    private void OnEnable()
    {
        ConstructGraphView();
        GenerateToolbar();
    }

    private void ConstructGraphView()
    {
        _graphView = new DialogueGraphView(this)
        {
            name = "Dialogue Graph"
        };
        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
    }

    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();

        // Selector del asset (¡este debe estar visible!)
        var assetField = new ObjectField("Graph")
        {
            objectType = typeof(DialogueGraph),
            allowSceneObjects = false
        };
        assetField.RegisterValueChangedCallback(evt =>
        {
            _asset = evt.newValue as DialogueGraph;
            if (_asset != null)
            {
                DialogueGraphSaveUtility.LoadGraph(_graphView, _asset);
            }
        });
        toolbar.Add(assetField); // <- importante: añade el ObjectField a la toolbar

        // Crear nodo nuevo
        var btnNewNode = new Button(() => _graphView.CreateNodeAtCenter())
        { text = "+ Nodo" };
        toolbar.Add(btnNewNode);

        // Guardar al asset (si no existe, crear uno)
        var btnSave = new Button(() =>
        {
            if (_asset == null)
            {
                var path = EditorUtility.SaveFilePanelInProject(
                    "Crear Dialogue Graph",
                    "NewDialogueGraph",
                    "asset",
                    "Elige carpeta para guardar el asset"
                );
                if (string.IsNullOrEmpty(path)) return;

                _asset = ScriptableObject.CreateInstance<DialogueGraph>();
                AssetDatabase.CreateAsset(_asset, path);
                AssetDatabase.SaveAssets();
            }

            DialogueGraphSaveUtility.SaveGraph(_graphView, _asset);
        })
        { text = "Guardar" };
        toolbar.Add(btnSave);

        // Recargar desde el asset seleccionado
        var btnLoad = new Button(() =>
        {
            if (_asset != null)
                DialogueGraphSaveUtility.LoadGraph(_graphView, _asset);
        })
        { text = "Recargar" };
        toolbar.Add(btnLoad);

        rootVisualElement.Add(toolbar);
    }

    private void OnDisable()
    {
        // (Opcional) auto-guardar si hay un asset asignado
        if (_graphView != null && _asset != null)
            DialogueGraphSaveUtility.SaveGraph(_graphView, _asset);

        if (_graphView != null)
            rootVisualElement.Remove(_graphView);
    }
}
#endif
