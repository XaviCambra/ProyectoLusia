#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class BackdropFrameView : GraphElement
{
    public BackdropFrameData Data { get; }

    private const float MinW = 120f;
    private const float MinH = 80f;
    private const float Handle = 8f;

    private TextField _title;
    private ColorField _color;

    // Evita guardar mientras estamos aplicando un rect por código (carga)
    private bool _suppressAutoSave;

    public BackdropFrameView(BackdropFrameData data)
    {
        Data = data ?? new BackdropFrameData(
            "Frame",
            new Color(0.2f, 0.6f, 1f, 0.15f),
            new Rect(50, 50, 420, 260)
        );

        // Estilo base
        name = "BackdropFrame";
        style.position = Position.Absolute;
        style.left = Data.rect.x;
        style.top = Data.rect.y;
        style.width = Mathf.Max(MinW, Data.rect.width);
        style.height = Mathf.Max(MinH, Data.rect.height);
        style.borderTopLeftRadius = 8;
        style.borderTopRightRadius = 8;
        style.borderBottomLeftRadius = 8;
        style.borderBottomRightRadius = 8;
        style.borderLeftWidth = 1;
        style.borderRightWidth = 1;
        style.borderTopWidth = 1;
        style.borderBottomWidth = 1;
        style.borderLeftColor = new Color(0, 0, 0, 0.35f);
        style.borderRightColor = new Color(0, 0, 0, 0.35f);
        style.borderTopColor = new Color(0, 0, 0, 0.35f);
        style.borderBottomColor = new Color(0, 0, 0, 0.35f);
        style.backgroundColor = Data.color; // incluye alfa

        capabilities = Capabilities.Movable | Capabilities.Resizable | Capabilities.Deletable | Capabilities.Selectable;

        // Siempre detrás de los nodos
        layer = -1000; // Siempre detrás de los edges
        SendToBack();

        // Header (para editar título/color)
        var header = new VisualElement { name = "header" };
        header.style.flexDirection = FlexDirection.Row;
        header.style.paddingLeft = 6;
        header.style.paddingRight = 6;
        header.style.paddingTop = 4;
        header.style.paddingBottom = 4;
        header.style.backgroundColor = new Color(0, 0, 0, 0.06f);
        header.style.unityFontStyleAndWeight = FontStyle.Bold;

        _title = new TextField { value = Data.title };
        _title.style.flexGrow = 1;
        _title.RegisterValueChangedCallback(e => Data.title = e.newValue);

        _color = new ColorField { value = Data.color, showAlpha = true };
        _color.RegisterValueChangedCallback(e =>
        {
            Data.color = e.newValue;
            style.backgroundColor = e.newValue;
        });

        header.Add(_title);
        header.Add(_color);
        EnableHeaderDrag(header);
        Add(header);

        // El cuerpo NO bloquea clics; solo el header y grips capturan
        pickingMode = PickingMode.Ignore;
        header.pickingMode = PickingMode.Position;

        // Handles de resize (8)
        AddResizeHandles();

        // Persistir rect solo cuando no estamos “silenciados”
        RegisterCallback<GeometryChangedEvent>(e =>
        {
            if (!_suppressAutoSave)
                SaveRect();
        });

        // Por si sueltas en el cuerpo sin pasar por header/grips
        RegisterCallback<PointerUpEvent>(_ => SaveRect());
    }

    private void EnableHeaderDrag(VisualElement header)
    {
        Vector2 startMousePanel = default;   // posición de ratón (panel)
        Rect startRect = default;
        bool dragging = false;

        header.RegisterCallback<PointerDownEvent>(e =>
        {
            if (e.button != 0) return; // solo LMB
            dragging = true;
            startMousePanel = e.position; // panel coords
            startRect = GetPosition();
            header.CapturePointer(e.pointerId);
            e.StopImmediatePropagation();
        });

        header.RegisterCallback<PointerMoveEvent>(e =>
        {
            if (!dragging || !header.HasPointerCapture(e.pointerId)) return;

            // Delta en coords de panel
            Vector2 deltaPanel = (Vector2)e.position - startMousePanel;

            // Compensar zoom del GraphView
            var gv = this.GetFirstAncestorOfType<GraphView>();
            var scale = gv != null ? gv.viewTransform.scale : Vector3.one;
            if (scale.x == 0f) scale.x = 1f;
            if (scale.y == 0f) scale.y = 1f;
            Vector2 delta = new(deltaPanel.x / scale.x, deltaPanel.y / scale.y);

            SetPosition(new Rect(
                startRect.x + delta.x,
                startRect.y + delta.y,
                startRect.width,
                startRect.height
            ));
            e.StopImmediatePropagation();
        });

        header.RegisterCallback<PointerUpEvent>(e =>
        {
            if (!dragging) return;
            dragging = false;
            header.ReleasePointer(e.pointerId);
            SaveRect(); // (1c) persistir al soltar
            e.StopImmediatePropagation();
        });
    }

    // ---- helpers de rect ----

    // Lee los valores “crudos” (sin redondeos) desde style.*
    private Rect GetStyleRect()
    {
        float left = style.left.value.value;
        float top = style.top.value.value;
        float width = style.width.value.value;
        float height = style.height.value.value;
        return new Rect(left, top, width, height);
    }

    // Aplica un rect sin disparar guardado (para cuando cargamos desde el asset)
    public void SetRectSilently(Rect r)
    {
        _suppressAutoSave = true;
        style.left = r.x;
        style.top = r.y;
        style.width = Mathf.Max(MinW, r.width);
        style.height = Mathf.Max(MinH, r.height);

        // Rehabilitar guardado tras el siguiente layout
        this.schedule.Execute(() => _suppressAutoSave = false);
    }

    // Guardado hacia el Data.rect usando los valores crudos
    private void SaveRect()
    {
        if (_suppressAutoSave) return;
        Data.rect = GetStyleRect();
    }

    // (1d) Usa valores crudos en lugar de resolvedStyle (evita “derrape” por redondeos)
    public override Rect GetPosition()
    {
        return GetStyleRect();
    }

    public override void SetPosition(Rect newPos)
    {
        style.left = newPos.x;
        style.top = newPos.y;
        style.width = Mathf.Max(MinW, newPos.width);
        style.height = Mathf.Max(MinH, newPos.height);
        SaveRect();
    }

    // ---------- RESIZE CUSTOM ----------

    enum Grip { N, S, E, W, NE, NW, SE, SW }

    void AddResizeHandles()
    {
        Add(GripElement(Grip.N));
        Add(GripElement(Grip.S));
        Add(GripElement(Grip.E));
        Add(GripElement(Grip.W));
        Add(GripElement(Grip.NE));
        Add(GripElement(Grip.NW));
        Add(GripElement(Grip.SE));
        Add(GripElement(Grip.SW));

        // Recolocar grips cuando cambie el tamaño
        RegisterCallback<GeometryChangedEvent>(_ => LayoutGrips());
    }

    VisualElement GripElement(Grip g)
    {
        var ve = new VisualElement { name = "grip-" + g };
        ve.pickingMode = PickingMode.Position;
        ve.style.position = Position.Absolute;
        ve.style.width = Handle;
        ve.style.height = Handle;
        ve.style.backgroundColor = new Color(0, 0, 0, 0.15f);

        // Cursor por tipo
        switch (g)
        {
            case Grip.N:
            case Grip.S:
                ve.style.cursor = new StyleCursor((StyleKeyword)MouseCursor.ResizeVertical);
                break;
            case Grip.E:
            case Grip.W:
                ve.style.cursor = new StyleCursor((StyleKeyword)MouseCursor.ResizeHorizontal);
                break;
            case Grip.NE:
            case Grip.SW:
                ve.style.cursor = new StyleCursor((StyleKeyword)MouseCursor.ResizeUpRight);
                break;
            case Grip.NW:
            case Grip.SE:
                ve.style.cursor = new StyleCursor((StyleKeyword)MouseCursor.ResizeUpLeft);
                break;
        }

        // Lógica de arrastre
        Vector2 startMouse = default;
        Rect startRect = default;
        bool dragging = false;

        ve.RegisterCallback<PointerDownEvent>(e =>
        {
            if (e.button != 0) return;
            dragging = true;
            startMouse = e.position;
            startRect = GetPosition();
            ve.CapturePointer(e.pointerId);
            e.StopImmediatePropagation();
        });

        ve.RegisterCallback<PointerMoveEvent>(e =>
        {
            if (!dragging || !ve.HasPointerCapture(e.pointerId)) return;

            Vector2 delta = (Vector2)e.position - startMouse;

            var x = startRect.x;
            var y = startRect.y;
            var w = startRect.width;
            var h = startRect.height;

            if (g == Grip.E || g == Grip.NE || g == Grip.SE) w = Mathf.Max(MinW, startRect.width + delta.x);
            if (g == Grip.S || g == Grip.SE || g == Grip.SW) h = Mathf.Max(MinH, startRect.height + delta.y);
            if (g == Grip.W || g == Grip.NW || g == Grip.SW)
            {
                float newW = Mathf.Max(MinW, startRect.width - delta.x);
                x = startRect.x + (startRect.width - newW);
                w = newW;
            }
            if (g == Grip.N || g == Grip.NE || g == Grip.NW)
            {
                float newH = Mathf.Max(MinH, startRect.height - delta.y);
                y = startRect.y + (startRect.height - newH);
                h = newH;
            }

            SetPosition(new Rect(x, y, w, h));
            LayoutGrips();
            e.StopImmediatePropagation();
        });

        ve.RegisterCallback<PointerUpEvent>(e =>
        {
            if (!dragging) return;
            dragging = false;
            ve.ReleasePointer(e.pointerId);
            SaveRect(); // (1c) persistir al soltar
            e.StopImmediatePropagation();
        });

        return ve;
    }

    void LayoutGrips()
    {
        float w = resolvedStyle.width;
        float h = resolvedStyle.height;

        void Place(string n, float left, float top)
        {
            var g = this.Q<VisualElement>(n);
            if (g == null) return;
            g.style.left = left;
            g.style.top = top;
        }

        Place("grip-N", (w - Handle) * 0.5f, -Handle * 0.5f);
        Place("grip-S", (w - Handle) * 0.5f, h - Handle * 0.5f);
        Place("grip-E", w - Handle * 0.5f, (h - Handle) * 0.5f);
        Place("grip-W", -Handle * 0.5f, (h - Handle) * 0.5f);

        Place("grip-NE", w - Handle * 0.5f, -Handle * 0.5f);
        Place("grip-NW", -Handle * 0.5f, -Handle * 0.5f);
        Place("grip-SE", w - Handle * 0.5f, h - Handle * 0.5f);
        Place("grip-SW", -Handle * 0.5f, h - Handle * 0.5f);
    }
}
#endif
