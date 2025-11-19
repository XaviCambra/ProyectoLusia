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

    // Título grande superpuesto dentro del frame
    private Label _overlayTitle;

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

        // ---------- HEADER (EDICIÓN) ----------
        var header = new VisualElement { name = "header" };
        header.style.flexDirection = FlexDirection.Row;
        header.style.paddingLeft = 6;
        header.style.paddingRight = 6;
        header.style.paddingTop = 4;
        header.style.paddingBottom = 4;
        header.style.backgroundColor = new Color(0, 0, 0, 0.0f); // transparente
        header.style.unityFontStyleAndWeight = FontStyle.Bold;

        _title = new TextField { value = Data.title };
        _title.style.flexGrow = 1;

        // Hacemos el campo visualmente "invisible"
        _title.style.backgroundColor = Color.clear;
        _title.style.borderBottomWidth = 0;
        _title.style.borderTopWidth = 0;
        _title.style.borderLeftWidth = 0;
        _title.style.borderRightWidth = 0;
        _title.style.unityFontStyleAndWeight = FontStyle.Normal;
        _title.style.fontSize = 11; // pequeñito, solo para editar
        _title.style.marginLeft = 0;
        _title.style.marginRight = 4;

        _title.RegisterValueChangedCallback(e =>
        {
            Data.title = e.newValue;
            if (_overlayTitle != null)
                _overlayTitle.text = e.newValue;
        });

        _color = new ColorField { value = Data.color, showAlpha = true };
        _color.RegisterValueChangedCallback(e =>
        {
            var newColor = e.newValue;
            Data.color = newColor;
            style.backgroundColor = newColor;

            if (_overlayTitle != null)
                _overlayTitle.style.color = GetTitleColorFromBackground(newColor);
        });

        header.Add(_title);
        header.Add(_color);
        EnableHeaderDrag(header);
        Add(header);

        // ---------- TÍTULO GRANDE SUPERPUESTO ----------
        _overlayTitle = new Label(Data.title)
        {
            name = "overlay-title"
        };
        _overlayTitle.style.position = Position.Absolute;
        _overlayTitle.style.left = 12;
        _overlayTitle.style.top = 32; // bajo el header
        _overlayTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        _overlayTitle.style.fontSize = 128; // tamaño grande
        _overlayTitle.style.color = GetTitleColorFromBackground(Data.color);
        _overlayTitle.pickingMode = PickingMode.Ignore; // no intercepta clics

        Add(_overlayTitle);
         
        // El cuerpo NO bloquea clics; solo el header y grips capturan
        pickingMode = PickingMode.Ignore;
        header.pickingMode = PickingMode.Position;

        // Handles de resize
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
            SaveRect();
            e.StopImmediatePropagation();
        });
    }

    // ---- helpers de rect ----

    private Color GetTitleColorFromBackground(Color bg)
    {
        // Luma perceptual (0 = muy oscuro, 1 = muy claro)
        float luma = 0.2126f * bg.r + 0.7152f * bg.g + 0.0722f * bg.b;

        // luma 0  → factor 2   (doble de intensidad)
        // luma 1  → factor 0.5 (mitad de intensidad)
        float factor = Mathf.Lerp(2f, 0.5f, luma);

        return new Color(
            Mathf.Clamp01(bg.r * factor),
            Mathf.Clamp01(bg.g * factor),
            Mathf.Clamp01(bg.b * factor),
            1f
        );
    }

    private Rect GetStyleRect()
    {
        float left = style.left.value.value;
        float top = style.top.value.value;
        float width = style.width.value.value;
        float height = style.height.value.value;
        return new Rect(left, top, width, height);
    }

    public void SetRectSilently(Rect r)
    {
        _suppressAutoSave = true;
        style.left = r.x;
        style.top = r.y;
        style.width = Mathf.Max(MinW, r.width);
        style.height = Mathf.Max(MinH, r.height);

        this.schedule.Execute(() => _suppressAutoSave = false);
    }

    private void SaveRect()
    {
        if (_suppressAutoSave) return;
        Data.rect = GetStyleRect();
    }

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
            SaveRect();
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
