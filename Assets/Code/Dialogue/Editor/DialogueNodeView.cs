// Editor/GraphView/DialogueNodeView.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Nodo visual del grafo de diálogos.
/// Contiene una lista reordenable de módulos (<see cref="IDialogueModule"/>)
/// y genera puertos de salida dinámicamente según el <see cref="ChoiceModule"/> presente.
/// </summary>
public class DialogueNodeView : Node
{
    // ─── Datos ────────────────────────────────────────────────────────────────
    public readonly DialogueNodeData Data;
    public Vector2 DefaultSize => new(400, 200);

    // ─── Dimensiones ──────────────────────────────────────────────────────────
    // M corresponde a margin, P a padding y S a spacing.</summary>
    private static float BottomMS = 4;
    private static float BottomMSTextField = BottomMS + 1;

    // ─── Callback externo ─────────────────────────────────────────────────────
    /// <summary>Se invoca cada vez que algún dato del nodo cambia (para marcar asset dirty).</summary>
    public Action OnDataChanged;

    // ─── UI interna ───────────────────────────────────────────────────────────
    private Port _input;
    private VisualElement _moduleListContainer;

    // ─── Constructor ──────────────────────────────────────────────────────────
    public DialogueNodeView(DialogueNodeData data)
    {
        Data = data;

        SetupChrome();
        BuildInputPort();
        BuildNodeHeader();
        BuildModuleList();
        RebuildOutputPorts();
        ScheduleAutoSize();
    }

    // ─── Setup visual básico ──────────────────────────────────────────────────
    private void SetupChrome()
    {
        var wNode  = 400;
        style.width    = wNode;
        style.minWidth = wNode;
        style.maxWidth = wNode;

        // Color de fondo según paleta
        if (DialogueNodeData.NodePalette != null && DialogueNodeData.NodePalette.Count > 0)
        {
            var col = DialogueNodeData.NodePalette[
                Mathf.Clamp(Data.bgColorIndex, 0, DialogueNodeData.NodePalette.Count - 1)].color;
            style.backgroundColor = col;
        }

        // Ocultar el divisor gris entre título y contenido
        var divider = this.Q("divider");
        if (divider != null) divider.style.display = DisplayStyle.None;

        // Eliminar el borde doble redondeado negro que añade Unity por defecto
        var nodeBorder = this.Q("node-border");
        if (nodeBorder != null)
        {
            nodeBorder.style.borderTopWidth    = 0;
            nodeBorder.style.borderBottomWidth = 0;
            nodeBorder.style.borderLeftWidth   = 0;
            nodeBorder.style.borderRightWidth  = 0;
        }

        // Eliminar el padding que Unity añade al contenedor principal
        var pContainer = 2;
        var mContainer = 2;
        mainContainer.style.paddingTop    = pContainer;
        mainContainer.style.paddingBottom = pContainer;
        mainContainer.style.paddingLeft   = pContainer;
        mainContainer.style.paddingRight  = pContainer;
        mainContainer.style.marginTop     = mContainer;
        mainContainer.style.marginBottom  = mContainer;
        mainContainer.style.marginLeft    = mContainer;
        mainContainer.style.marginRight   = mContainer;

        // Redondear esquinas del nodo y recortar hijos para que respeten el radio
        const float r = 10f;
        style.borderTopLeftRadius     = r;
        style.borderTopRightRadius    = r;
        style.borderBottomLeftRadius  = r;
        style.borderBottomRightRadius = r;
        style.overflow = Overflow.Hidden;

        titleContainer.style.backgroundColor = new Color(0f, 0f, 0f, 0.25f);
        titleContainer.style.marginLeft      = 4;
        titleContainer.style.marginRight     = 4;
        inputContainer.style.backgroundColor  = new Color(0f, 0f, 0f, 0.10f);
        outputContainer.style.backgroundColor = new Color(0f, 0f, 0f, 0.15f);

        // Margen lateral al contenedor de puertos para que no quede pegado al borde
        var topContainer = this.Q("top");
        if (topContainer != null)
        {
            topContainer.style.marginLeft  = 4;
            topContainer.style.marginRight = 4;
        }

        // Botón eliminar en la cabecera
        var deleteBtn = new Button(DeleteSelf) { text = "✕" };
        deleteBtn.style.width  = 20;
        deleteBtn.style.height = 20;
        titleContainer.Add(deleteBtn);
    }

    // ─── Puerto de entrada ────────────────────────────────────────────────────
    private void BuildInputPort()
    {
        _input = PortUtils.CreatePort(this, Direction.Input, Port.Capacity.Multi, "In");
        inputContainer.Add(_input);
    }

    // ─── Cabecera del nodo (palette + isStart) ────────────────────────────────
    private void BuildNodeHeader()
    {
        var header = new VisualElement();
        header.style.paddingLeft = 0;
        header.style.paddingTop  = 4;

        // Selector de color de paleta
        var paletteRow = new VisualElement();
        paletteRow.style.flexDirection = FlexDirection.Row;
        paletteRow.style.flexWrap      = Wrap.Wrap;

        if (DialogueNodeData.NodePalette != null)
        {
            for (int i = 0; i < DialogueNodeData.NodePalette.Count; i++)
            {
                int capturedIndex = i;
                var swatch = new Button(() =>
                {
                    Data.bgColorIndex = capturedIndex;
                    Data.OnValidate();
                    style.backgroundColor = Data.bgColor;
                    Notify();
                });
                swatch.style.width             = 20;
                swatch.style.height            = 20;
                swatch.style.backgroundColor   = DialogueNodeData.NodePalette[i].color;
                swatch.style.marginTop         = 0;
                swatch.style.marginRight       = 0;
                swatch.style.marginBottom      = 0;
                paletteRow.Add(swatch);
                paletteRow.style.marginBottom  = BottomMS;
            }
        }
        header.Add(paletteRow);

        // Nodo de inicio
        var startToggle = new Toggle("Start Node") { value = Data.isStart };
        startToggle.RegisterValueChangedCallback(e =>
        {
            Data.isStart = e.newValue;
            startIdField.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            Notify();
        });
        startToggle.style.marginBottom = BottomMS;
        header.Add(startToggle);

        startIdField = new TextField("Start ID") { value = Data.startId };
        startIdField.style.display = Data.isStart ? DisplayStyle.Flex : DisplayStyle.None;
        startIdField.RegisterValueChangedCallback(e => { Data.startId = e.newValue; Notify(); });
        startIdField.style.marginBottom = BottomMS+1;
        header.Add(startIdField);

        mainContainer.Insert(0, header);
    }
    private TextField startIdField;

    // ─── Lista de módulos ─────────────────────────────────────────────────────
    private void BuildModuleList()
    {
        var wrapper = new VisualElement();
        wrapper.style.paddingLeft   = 4;
        wrapper.style.paddingRight  = 4;
        wrapper.style.paddingBottom = 4;

        // Contenedor de items
        _moduleListContainer = new VisualElement();
        wrapper.Add(_moduleListContainer);

        // Botón "+ Add Module" con dropdown
        var addBtn = new Button(() => ShowAddModuleMenu()) { text = "+ Add Module" };
        addBtn.style.marginTop = 6;
        wrapper.Add(addBtn);

        mainContainer.Add(wrapper);
        RefreshModuleList();
    }

    private void RefreshModuleList()
    {
        _moduleListContainer.Clear();

        for (int i = 0; i < Data.modules.Count; i++)
        {
            var module = Data.modules[i];
            if (module == null) continue;
            int capturedIndex = i;
            _moduleListContainer.Add(BuildModuleItem(module, capturedIndex));
        }
    }

    private VisualElement BuildModuleItem(IDialogueModule module, int index)
    {
        var item = new VisualElement();
        item.style.borderBottomWidth = 1;
        item.style.borderBottomColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        item.style.marginBottom      = 2;

        // ── Header ──
        var header = new VisualElement();
        header.style.flexDirection  = FlexDirection.Row;
        header.style.alignItems     = Align.Center;
        header.style.backgroundColor = new Color(0f, 0f, 0f, 0.15f);
        header.style.paddingLeft    = 4;

        // Flechas de reorden
        var upBtn = new Button(() =>
        {
            if (index > 0)
            {
                var tmp = Data.modules[index - 1];
                Data.modules[index - 1] = Data.modules[index];
                Data.modules[index] = tmp;
                RefreshModuleList();
                RebuildOutputPorts();
                Notify();
            }
        }) { text = "▲" };
        upBtn.style.width  = 20;
        upBtn.style.height = 20;

        var downBtn = new Button(() =>
        {
            if (index < Data.modules.Count - 1)
            {
                var tmp = Data.modules[index + 1];
                Data.modules[index + 1] = Data.modules[index];
                Data.modules[index] = tmp;
                RefreshModuleList();
                RebuildOutputPorts();
                Notify();
            }
        }) { text = "▼" };
        downBtn.style.width  = 20;
        downBtn.style.height = 20;

        // Nombre del módulo
        var nameLabel = new Label(module.DisplayName);
        nameLabel.style.flexGrow  = 1;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.marginLeft = 4;

        // RunMode (excepto ChoiceModule que siempre es Blocking)
        VisualElement blocksElement;
        if (module is ChoiceModule)
        {
            var fixedLabel = new Label("Blocking ✓");
            fixedLabel.style.color    = new Color(0.6f, 0.6f, 0.6f, 1f);
            fixedLabel.style.fontSize = 10;
            fixedLabel.style.width    = 90;
            blocksElement = fixedLabel;
        }
        else
        {
            var runModeBtn = new Button();
            runModeBtn.text = module.RunMode.ToString();
            runModeBtn.style.width  = 110;
            runModeBtn.style.height = 20;
            runModeBtn.clicked += () =>
            {
                var menu = new GenericMenu();
                foreach (ModuleRunMode mode in Enum.GetValues(typeof(ModuleRunMode)))
                {
                    var capturedMode = mode;
                    menu.AddItem(new GUIContent(mode.ToString()), module.RunMode == mode, () =>
                    {
                        module.RunMode    = capturedMode;
                        runModeBtn.text   = capturedMode.ToString();
                        Notify();
                    });
                }
                menu.ShowAsContext();
            };
            blocksElement = runModeBtn;
        }

        // Botón eliminar
        var deleteBtn = new Button(() =>
        {
            Data.modules.RemoveAt(index);
            RefreshModuleList();
            RebuildOutputPorts();
            Notify();
        }) { text = "✕" };
        deleteBtn.style.width  = 22;
        deleteBtn.style.height = 22;

        header.Add(upBtn);
        header.Add(downBtn);
        header.Add(nameLabel);
        header.Add(blocksElement);
        header.Add(deleteBtn);
        item.Add(header);

        // ── Body (drawer) ──
        var body = new VisualElement();
        body.style.paddingLeft = 8;

        Action onChanged = () =>
        {
            // Si el módulo es ChoiceModule, reconstruimos los puertos
            if (module is ChoiceModule)
                RebuildOutputPorts();
            Notify();
        };

        body.Add(ModuleDrawerRegistry.Draw(module, onChanged));
        item.Add(body);
        return item;
    }

    // ─── Puertos de salida dinámicos ──────────────────────────────────────────
    public void RebuildOutputPorts()
    {
        var graphView = GetFirstAncestorOfType<DialogueGraphView>();

        // 1. Capturar conexiones existentes por nombre de puerto (portName → puerto de entrada destino)
        var preserved = new Dictionary<string, Port>();
        var edgesToRemove = new List<Edge>();
        foreach (var child in outputContainer.Children())
        {
            var outPort = child as Port
                ?? (child as VisualElement)?.Children().OfType<Port>().FirstOrDefault();
            if (outPort == null) continue;
            foreach (var edge in outPort.connections)
            {
                if (edge.input != null)
                    preserved[outPort.portName] = edge.input;
                edgesToRemove.Add(edge);
            }
        }

        // 2. Eliminar edges huérfanos del GraphView antes de limpiar los puertos
        if (graphView != null && edgesToRemove.Count > 0)
            graphView.DeleteElements(edgesToRemove);

        outputContainer.Clear();

        // 3. Reconstruir puertos
        var choiceModule = Data.GetChoiceModule();
        if (choiceModule != null)
        {
            foreach (var choice in choiceModule.choices)
            {
                if (choice == null) continue;
                var portName = string.IsNullOrEmpty(choice.portName)
                    ? $"choice_{choiceModule.choices.IndexOf(choice)}"
                    : choice.portName;

                var port = PortUtils.CreatePort(this, Direction.Output, Port.Capacity.Single, portName);

                var label = new Label(string.IsNullOrEmpty(choice.choiceText) ? portName : choice.choiceText);
                label.style.marginRight = 4;

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems    = Align.Center;
                row.Add(label);
                row.Add(port);
                outputContainer.Add(row);
            }
        }
        else
        {
            var nextPort = PortUtils.CreatePort(this, Direction.Output, Port.Capacity.Single, "Next");
            outputContainer.Add(nextPort);
        }

        // 4. Restaurar conexiones por nombre de puerto
        if (graphView != null && preserved.Count > 0)
        {
            foreach (var child in outputContainer.Children())
            {
                var outPort = child as Port
                    ?? (child as VisualElement)?.Children().OfType<Port>().FirstOrDefault();
                if (outPort == null) continue;
                if (!preserved.TryGetValue(outPort.portName, out var targetInput)) continue;

                var newEdge = outPort.ConnectTo(targetInput);
                graphView.AddElement(newEdge);
            }
        }

        RefreshPorts();
    }

    // ─── Menú "Add Module" ─────────────────────────────────────────────────────
    private void ShowAddModuleMenu()
    {
        var menu = new GenericMenu();
        foreach (var kv in ModuleDrawerRegistry.AvailableModuleTypes)
        {
            var displayName  = kv.Key;
            var moduleType   = kv.Value;
            menu.AddItem(new GUIContent(displayName), false, () =>
            {
                var instance = (IDialogueModule)Activator.CreateInstance(moduleType);
                Data.modules.Add(instance);
                RefreshModuleList();
                RebuildOutputPorts();
                Notify();
            });
        }
        menu.ShowAsContext();
    }

    // ─── Eliminar nodo ────────────────────────────────────────────────────────
    private void DeleteSelf()
    {
        if (GetFirstAncestorOfType<DialogueGraphView>() is { } graphView)
            graphView.DeleteElements(new[] { this });
    }

    // ─── Notify ───────────────────────────────────────────────────────────────
    private void Notify() => OnDataChanged?.Invoke();

    // ─── Posicionamiento ──────────────────────────────────────────────────────
    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        Data.nodeRect = newPos;
    }

    private void ScheduleAutoSize()
    {
        schedule.Execute(() =>
        {
            var rect = Data.nodeRect;
            if (rect.width <= 1f) rect.width  = 400f;
            if (rect.height <= 1f) rect.height = 200f;
            base.SetPosition(rect);
        }).ExecuteLater(0);
    }
}
#endif
