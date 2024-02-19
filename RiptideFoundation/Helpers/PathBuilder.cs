namespace RiptideFoundation.Helpers;

public partial struct PathBuilder {
    private readonly MeshBuilder _builder;
    private readonly VertexWriter<Vertex> _writer;
    private readonly IndexFormat _indexFormat;

    private readonly List<PathOperation> _operations;

    public PathBuilder(MeshBuilder builder, VertexWriter<Vertex> writer, IndexFormat indexFormat) {
        _builder = builder;
        _writer = writer;
        _indexFormat = indexFormat is not IndexFormat.UInt16 and not IndexFormat.UInt32 ? IndexFormat.UInt16 : indexFormat;

        _operations = new(16);
    }

    [UnscopedRef]
    public ref PathBuilder SetColor(Color32 color) {
        _operations.Add(new() {
            Type = OperationType.SetColor,
            Color = color,
        });

        return ref this;
    }
    
    [UnscopedRef]
    public ref PathBuilder SetThickness(float thickness) {
        _operations.Add(new() {
            Type = OperationType.SetThickness,
            Thickness = thickness,
        });

        return ref this;
    }

    [UnscopedRef]
    public ref PathBuilder Begin() {
        _operations.Clear();

        return ref this;
    }

    [UnscopedRef]
    public ref PathBuilder MoveTo(Vector2 destination) {
        _operations.Add(new() {
            Type = OperationType.MoveTo,
            Move = new() {
                Destination = destination,
            },
        });

        return ref this;
    }
    
    [UnscopedRef]
    public ref PathBuilder LineTo(Vector2 destination) {
        _operations.Add(new() {
            Type = OperationType.LineTo,
            Line = new() {
                Destination = destination,
            },
        });

        return ref this;
    }

    [UnscopedRef]
    public ref PathBuilder HorizontalLineTo(float x) {
        _operations.Add(new() {
            Type = OperationType.HorizontalLine,
            HorizontalLine = new() {
                Destination = x,
            },
        });

        return ref this;
    }
    
    [UnscopedRef]
    public ref PathBuilder VerticalLineTo(float x) {
        _operations.Add(new() {
            Type = OperationType.VerticalLine,
            VerticalLine = new() {
                Destination = x,
            },
        });

        return ref this;
    }

    [UnscopedRef]
    public ref PathBuilder BezierTo(Vector2 control, Vector2 destination) {
        _operations.Add(new() {
            Type = OperationType.QuadraticBezier,
            QuadraticBezier = new(control, destination),
        });

        return ref this;
    }

    [UnscopedRef]
    public ref PathBuilder BezierTo(Vector2 startControl, Vector2 endControl, Vector2 destination) {
        _operations.Add(new() {
            Type = OperationType.CubicBezier,
            CubicBezier = new(startControl, endControl, destination),
        });

        return ref this;
    }

    [UnscopedRef]
    public ref PathBuilder CloseSubpath(bool loop = false) {
        _operations.Add(new() {
            Type = OperationType.Close,
            Close = new() {
                Loop = loop,
            },
        });

        return ref this;
    }
    
    [UnscopedRef]
    public ref PathBuilder End() {
        Build(_builder, CollectionsMarshal.AsSpan(_operations), 1, Color32.White, _writer, _indexFormat);

        return ref this;
    }
    
    public readonly record struct Vertex(Vector2 Position, Color32 Color);
}