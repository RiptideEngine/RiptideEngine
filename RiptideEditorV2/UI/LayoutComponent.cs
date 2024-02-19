namespace RiptideEditorV2.UI;

public sealed class LayoutComponent {
    private readonly InterfaceElement _element;

    private float _x, _y, _w, _h;

    public float X {
        get => _x;
        set {
            _x = value;
            _element.Invalidate(InvalidationFlags.Layout);
        }
    }
    public float Y {
        get => _y;
        set {
            _y = value;
            _element.Invalidate(InvalidationFlags.Layout);
        }
    }
    public float Width {
        get => _w;
        set {
            _w = value;
            _element.Invalidate(InvalidationFlags.Layout);
        }
    }
    public float Height {
        get => _h;
        set {
            _h = value;
            _element.Invalidate(InvalidationFlags.Layout);
        }
    }

    internal LayoutComponent(InterfaceElement element) {
        _element = element;
    }
}