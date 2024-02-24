namespace RiptideFoundation.Helpers;

public partial struct PathBuilder {
    private readonly MeshBuilder _builder;
    private readonly VertexWriter<PathBuilding.Vertex> _writer;
    private readonly IndexFormat _indexFormat;

    private readonly List<PathOperation> _operations;
    private Vector2 _position;

    public PathBuilder(MeshBuilder builder, VertexWriter<PathBuilding.Vertex> writer, IndexFormat indexFormat) {
        _builder = builder;
        _writer = writer;
        _indexFormat = indexFormat is not IndexFormat.UInt16 and not IndexFormat.UInt32 ? IndexFormat.UInt16 : indexFormat;

        _operations = new(16);
    }
    
    [UnscopedRef]
    public ref PathBuilder SetColor(Color32 color) {
        _operations.Add(new() {
            Type = PathOperationType.SetColor,
            Color = color,
        });

        return ref this;
    }
    
    [UnscopedRef]
    public ref PathBuilder SetThickness(float thickness) {
        _operations.Add(new() {
            Type = PathOperationType.SetThickness,
            Thickness = thickness,
        });

        return ref this;
    }

    [UnscopedRef]
    public ref PathBuilder Begin() {
        _operations.Clear();
        _position = Vector2.Zero;

        return ref this;
    }

    [UnscopedRef]
    public ref PathBuilder MoveTo(Vector2 destination) {
        _operations.Add(new() {
            Type = PathOperationType.MoveTo,
            Move = new() {
                Destination = destination,
            },
        });

        _position = destination;

        return ref this;
    }
    
    [UnscopedRef]
    public ref PathBuilder LineTo(Vector2 destination) {
        _operations.Add(new() {
            Type = PathOperationType.LineTo,
            Line = new() {
                Destination = destination,
            },
        });

        _position = destination;

        return ref this;
    }

    [UnscopedRef]
    public ref PathBuilder HorizontalLineTo(float x) {
        _operations.Add(new() {
            Type = PathOperationType.LineTo,
            Line = new() {
            }
        });

        _position.X = x;

        return ref this;
    }
    
    [UnscopedRef]
    public ref PathBuilder VerticalLineTo(float x) {
        _operations.Add(new() {
            Type = PathOperationType.VerticalLine,
            VerticalLine = new() {
                Destination = x,
            },
        });

        return ref this;
    }

    [UnscopedRef]
    public ref PathBuilder BezierTo(Vector2 control, Vector2 destination) {
        _operations.Add(new() {
            Type = PathOperationType.QuadraticBezier,
            QuadraticBezier = new(control, destination),
        });

        return ref this;
    }

    [UnscopedRef]
    public ref PathBuilder BezierTo(Vector2 startControl, Vector2 endControl, Vector2 destination) {
        _operations.Add(new() {
            Type = PathOperationType.CubicBezier,
            CubicBezier = new(startControl, endControl, destination),
        });

        return ref this;
    }

    [UnscopedRef]
    public ref PathBuilder CloseSubpath(PathCapType capType = PathCapType.Butt) {
        _operations.Add(new() {
            Type = PathOperationType.Close,
            Close = new() {
                CapType = capType is PathCapType.Butt or PathCapType.Round or PathCapType.Square ? capType : PathCapType.Butt,
                Loop = false,
            },
        });

        return ref this;
    }
    
    [UnscopedRef]
    public ref PathBuilder End() {
        PathBuilding.Build(_builder, CollectionsMarshal.AsSpan(_operations), 1, Color32.White, _writer, _indexFormat);

        return ref this;
    }
}