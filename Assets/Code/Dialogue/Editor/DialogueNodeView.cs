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

    // ─── Estilo ───────────────────────────────────────────────────────────────
    // Nodo
    private const float NodeWidth          = 400f;
    private const float NodeBorderRadius   = 10f;

    // mainContainer
    private const float ContainerPadding   = 1f;
    private const float ContainerMargin    = 1f;

    // titleContainer / topContainer
    private const float TitleTopMargin     = 4f;
    private const float TitleSideMargin    = 4f;
    private const float PortsSideMargin    = 4f;
    private const float PortsBottomMargin  = 4f;

    // Cabecera del nodo
    private const float HeaderPaddingTop        = 4f;
    private const float SwatchSize              = 32f;
    private const float HeaderItemMarginBottom  = 4f;
    private const float StartIdFieldMarginTop   = 4f;

    // Wrapper de módulos
    private const float ModuleListPadding  = 4f;
    private const float AddBtnMarginTop    = 6f;

    // Item de módulo
    private const float InnerBlockRadius   = 4f;
    private const float ModuleItemMargin   = 4f;
    private const float ModuleHeaderPaddingLeft = 4f;
    private const float ModuleBodyPaddingLeft   = 8f;
    private const float ModuleNameMarginLeft    = 4f;
    private const float BtnSmallSize       = 20f;
    private const float BtnDeleteSize      = 22f;
    private const float RunModeBtnWidth    = 110f;
    private const float BlockingLabelWidth = 90f;
    private const float BlockingLabelFontSize = 10f;
    private const float ChoiceLabelMarginRight = 4f;

    // Colores
    private static readonly Color ColTitleBg    = new(0f, 0f, 0f, 0.25f);
    private static readonly Color ColInputBg    = new(0f, 0f, 0f, 0.10f);
    private static readonly Color ColOutputBg   = new(0f, 0f, 0f, 0.15f);
    private static readonly Color ColModuleItemBg     = new(0f, 0f, 0f, 0.18f);
    private static readonly Color ColModuleHeaderBg   = new(0f, 0f, 0f, 0.15f);
    private static readonly Color ColBlockingLabel    = new(0.6f, 0.6f, 0.6f, 1f);

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
        style.width    = NodeWidth;
        style.minWidth = NodeWidth;
        style.maxWidth = NodeWidth;

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
        mainContainer.style.paddingTop    = ContainerPadding;
        mainContainer.style.paddingBottom = ContainerPadding;
        mainContainer.style.paddingLeft   = ContainerPadding;
        mainContainer.style.paddingRight  = ContainerPadding;
        mainContainer.style.marginTop     = ContainerMargin;
        mainContainer.style.marginBottom  = ContainerMargin;
        mainContainer.style.marginLeft    = ContainerMargin;
        mainContainer.style.marginRight   = ContainerMargin;

        // Redondear esquinas del nodo y recortar hijos para que respeten el radio
        style.borderTopLeftRadius     = NodeBorderRadius;
        style.borderTopRightRadius    = NodeBorderRadius;
        style.borderBottomLeftRadius  = NodeBorderRadius;
        style.borderBottomRightRadius = NodeBorderRadius;
        style.overflow = Overflow.Hidden;

        // titleContainer
        titleContainer.style.backgroundColor      = ColTitleBg;
        titleContainer.style.marginTop            = TitleTopMargin;
        titleContainer.style.marginLeft           = TitleSideMargin;
        titleContainer.style.marginRight          = TitleSideMargin;
        titleContainer.style.borderTopLeftRadius  = InnerBlockRadius;
        titleContainer.style.borderTopRightRadius = InnerBlockRadius;

        // inputContainer: esquinas izquierdas redondeadas
        inputContainer.style.backgroundColor        = ColInputBg;
        inputContainer.style.borderBottomLeftRadius = InnerBlockRadius;

        // outputContainer: esquinas derechas redondeadas
        outputContainer.style.backgroundColor         = ColOutputBg;
        outputContainer.style.borderBottomRightRadius = InnerBlockRadius;

        // topContainer (#top): fila In/Next
        var topContainer = this.Q("top");
        if (topContainer != null)
        {
            topContainer.style.marginLeft              = PortsSideMargin;
            topContainer.style.marginRight             = PortsSideMargin;
            topContainer.style.marginBottom            = PortsBottomMargin;
            topContainer.style.borderBottomLeftRadius  = InnerBlockRadius;
            topContainer.style.borderBottomRightRadius = InnerBlockRadius;
            topContainer.style.overflow                = Overflow.Hidden;
        }

        // Botón eliminar nodo
        var deleteBtn = new Button(DeleteSelf) { text = "✕" };
        deleteBtn.style.width  = BtnSmallSize;
        deleteBtn.style.height = BtnSmallSize;

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
        header.style.paddingTop = HeaderPaddingTop;

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
                swatch.style.width           = SwatchSize;
                swatch.style.marginRight     = 2;
                swatch.style.height          = SwatchSize;
                swatch.style.backgroundColor = DialogueNodeData.NodePalette[i].color;
                paletteRow.Add(swatch);
                paletteRow.style.marginBottom = HeaderItemMarginBottom;
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
        startToggle.style.marginBottom = HeaderItemMarginBottom;
        header.Add(startToggle);

        startIdField = new TextField("Start ID") { value = Data.startId };
        startIdField.style.display = Data.isStart ? DisplayStyle.Flex : DisplayStyle.None;
        startIdField.RegisterValueChangedCallback(e => { Data.startId = e.newValue; Notify(); });
        startIdField.style.marginTop = StartIdFieldMarginTop;
        startIdField.style.marginBottom = HeaderItemMarginBottom;
        startIdField.style.marginRight = TitleSideMargin;
        header.Add(startIdField);

        mainContainer.Insert(0, header);
    }
    private TextField startIdField;

    // ─── Lista de módulos ─────────────────────────────────────────────────────
    private void BuildModuleList()
    {
        var wrapper = new VisualElement();
        wrapper.style.paddingLeft   = ModuleListPadding;
        wrapper.style.paddingRight  = ModuleListPadding;
        wrapper.style.paddingBottom = ModuleListPadding;

        // Contenedor de items
        _moduleListContainer = new VisualElement();
        wrapper.Add(_moduleListContainer);

        // Botón "+ Add Module" con dropdown
        var addBtn = new Button(() => ShowAddModuleMenu()) { text = "+ Add Module" };
        addBtn.style.marginTop = AddBtnMarginTop;
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
        item.style.backgroundColor         = ColModuleItemBg;
        item.style.borderTopLeftRadius     = InnerBlockRadius;
        item.style.borderTopRightRadius    = InnerBlockRadius;
        item.style.borderBottomLeftRadius  = InnerBlockRadius;
        item.style.borderBottomRightRadius = InnerBlockRadius;
        item.style.marginTop    = ModuleItemMargin;
        item.style.marginBottom = ModuleItemMargin;
        item.style.overflow     = Overflow.Hidden;

        // ── Header ──
        var header = new VisualElement();
        header.style.flexDirection        = FlexDirection.Row;
        header.style.alignItems           = Align.Center;
        header.style.backgroundColor      = ColModuleHeaderBg;
        header.style.paddingLeft          = ModuleHeaderPaddingLeft;
        header.style.borderTopLeftRadius  = InnerBlockRadius;
        header.style.borderTopRightRadius = InnerBlockRadius;

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
        upBtn.style.width  = BtnSmallSize;
        upBtn.style.height = BtnSmallSize;

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
        downBtn.style.width  = BtnSmallSize;
        downBtn.style.height = BtnSmallSize;

        // Nombre del módulo
        var nameLabel = new Label(module.DisplayName);
        nameLabel.style.flexGrow  = 1;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.marginLeft = ModuleNameMarginLeft;

        // RunMode (excepto ChoiceModule que siempre es Blocking)
        VisualElement blocksElement;
        if (module is ChoiceModule)
        {
            var fixedLabel = new Label("Blocking ✓");
            fixedLabel.style.color    = ColBlockingLabel;
            fixedLabel.style.fontSize = BlockingLabelFontSize;
            fixedLabel.style.width    = BlockingLabelWidth;
            blocksElement = fixedLabel;
        }
        else
        {
            var runModeBtn = new Button();
            runModeBtn.text = module.RunMode.ToString();
            runModeBtn.style.width  = RunModeBtnWidth;
            runModeBtn.style.height = BtnSmallSize;
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
        deleteBtn.style.width  = BtnDeleteSize;
        deleteBtn.style.height = BtnDeleteSize;

        header.Add(upBtn);
        header.Add(downBtn);
        header.Add(nameLabel);
        header.Add(blocksElement);
        header.Add(deleteBtn);
        item.Add(header);

        // ── Body (drawer) ──
        var body = new VisualElement();
        body.style.paddingLeft = ModuleBodyPaddingLeft;

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
                label.style.marginRight = ChoiceLabelMarginRight;

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
