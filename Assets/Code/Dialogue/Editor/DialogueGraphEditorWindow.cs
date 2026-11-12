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
    private bool _isLoading = false;   // <-- usado para envolver cargas

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

    public void MarkAssetDirtyAndSave()
    {
        if (_isLoading) return;
        if (_asset != null && _graphView != null)
            DialogueGraphSaveUtility.SaveGraph(_graphView, _asset);
    }

    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();

        // Selector del asset
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
                _isLoading = true;
                DialogueGraphSaveUtility.LoadGraph(_graphView, _asset);
                // Asegurar que los marcos (si existen) se envíen detrás de los nodos
                _graphView.EnsureFramesBehindNodes();
                _isLoading = false;
            }
        });
        toolbar.Add(assetField);

        // ---- NUEVO: crear un Backdrop/Marco centrado en la vista ----
        var btnNewFrame = new Button(() =>
        {
            if (_isLoading) return;
            _graphView.CreateBackdropFrameCentered("Frame");
            _graphView.EnsureFramesBehindNodes();
        })
        { text = "+ Marco" };
        toolbar.Add(btnNewFrame);

        // Crear nodo nuevo
        var btnNewNode = new Button(() =>
        {
            if (_isLoading) return;
            _graphView.CreateNodeAtCenter();
        })
        { text = "+ Nodo" };
        toolbar.Add(btnNewNode);

        // Crear diálogo nuevo
        var newDialogueButton = new Button(() =>
        {
            var newAsset = ScriptableObject.CreateInstance<DialogueGraph>();
            string path = EditorUtility.SaveFilePanelInProject(
                "Nuevo Dialogue Graph",
                "NewDialogueGraph",
                "asset",
                "Selecciona la ubicación para guardar el nuevo DialogueGraph."
            );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(newAsset, path);
                AssetDatabase.SaveAssets();

                // Actualizar campo visual (usa la variable local 'assetField')
                assetField.value = newAsset;

                // Actualizar referencia interna
                _asset = newAsset;

                // Cargar el grafo recién creado
                _isLoading = true;
                DialogueGraphSaveUtility.LoadGraph(_graphView, _asset);
                _graphView.EnsureFramesBehindNodes();
                _isLoading = false;
            }
        })
        { text = "+ Dialogue" };
        toolbar.Add(newDialogueButton);

        // Guardar al asset (si no existe, crear uno)
        var btnSave = new Button(() =>
        {
            if (_isLoading) return;
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
            EditorUtility.SetDirty(_asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Validar tras guardar: solo dice algo si hay problemas
            RunValidation(logWhenClean: false);
        })
        { text = "Guardar" };
        toolbar.Add(btnSave);

        // Validar el grafo (ver DialogueGraphValidator) y seleccionar los nodos con problemas
        var btnValidate = new Button(() =>
        {
            if (_isLoading || _asset == null) return;
            MarkAssetDirtyAndSave(); // sincroniza vista -> asset antes de validar
            RunValidation(logWhenClean: true);
        })
        { text = "Validar" };
        toolbar.Add(btnValidate);

        // Recargar desde el asset seleccionado
        var btnLoad = new Button(() =>
        {
            if (_asset != null)
            {
                _isLoading = true;
                DialogueGraphSaveUtility.LoadGraph(_graphView, _asset);
                _graphView.EnsureFramesBehindNodes();
                _isLoading = false;
            }
        })
        { text = "Recargar" };
        toolbar.Add(btnLoad);

        rootVisualElement.Add(toolbar);
    }

    /// <summary>Valida el asset abierto, vuelca el resultado a la consola y selecciona en
    /// la vista los nodos afectados para encontrarlos rapido.</summary>
    private void RunValidation(bool logWhenClean)
    {
        if (_asset == null || _graphView == null) return;

        var issues = DialogueGraphValidator.ValidateWithDetectedEngine(_asset);
        if (issues.Count == 0)
        {
            if (logWhenClean) Debug.Log($"[DialogueGraphValidator] '{_asset.name}': sin problemas.", _asset);
            return;
        }

        DialogueGraphValidator.Log(_asset, issues);

        var badGuids = new System.Collections.Generic.HashSet<string>();
        foreach (var i in issues)
            if (!string.IsNullOrEmpty(i.nodeGuid)) badGuids.Add(i.nodeGuid);

        _graphView.ClearSelection();
        _graphView.nodes.ForEach(n =>
        {
            if (n is DialogueNodeView nv && nv.Data != null && badGuids.Contains(nv.Data.GUID))
                _graphView.AddToSelection(nv);
        });
    }

    private void OnDisable()
    {
        // ❌ Se elimina el auto-guardado para evitar sobrescrituras accidentales
        if (_graphView != null)
            rootVisualElement.Remove(_graphView);
    }
}
#endif
