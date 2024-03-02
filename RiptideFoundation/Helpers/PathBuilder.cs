namespace RiptideFoundation.Helpers;

public partial struct PathBuilder {
    private readonly MeshBuilder _builder;
    private readonly VertexWriter<PathBuilding.Vertex> _writer;
    private readonly IndexFormat _indexFormat;

    private readonly List<PathOperation> _operations;
    private Vector2 _position;

    private PathBuildingConfiguration _config;

    public PathBuilder(MeshBuilder builder, VertexWriter<PathBuilding.Vertex> writer, IndexFormat indexFormat) {
        _builder = builder;
        _writer = writer;
        _indexFormat = indexFormat is not IndexFormat.UInt16 and not IndexFormat.UInt32 ? IndexFormat.UInt16 : indexFormat;
        _config = PathBuildingConfiguration.Default;

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
    public ref PathBuilder Begin(PathBuildingConfiguration config) {
        if (_operations.Count != 0) {
            PathBuilding.Build(_builder, CollectionsMarshal.AsSpan(_operations), 1, Color32.White, config, _writer, _indexFormat);
        }
        
        _operations.Clear();
        _position = Vector2.Zero;
        _config = config;

        return ref this;
    }
    [UnscopedRef] public ref PathBuilder Begin() => ref Begin(PathBuildingConfiguration.Default);

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
    [UnscopedRef] public ref PathBuilder MoveTo(float x, float y) => ref MoveTo(new(x, y));

    [UnscopedRef] public ref PathBuilder MoveRelative(Vector2 offset) => ref MoveTo(_position + offset);
    [UnscopedRef] public ref PathBuilder MoveRelative(float xOffset, float yOffset) => ref MoveRelative(new(xOffset, yOffset));
    
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
    [UnscopedRef] public ref PathBuilder LineTo(float x, float y) => ref LineTo(new(x, y));

    [UnscopedRef] public ref PathBuilder LineRelative(Vector2 offset) => ref LineTo(_position + offset);
    [UnscopedRef] public ref PathBuilder LineRelative(float xOffset, float yOffset) => ref LineRelative(new(xOffset, yOffset));
    
    [UnscopedRef] public ref PathBuilder HorizontalLineTo(float x) => ref LineTo(new(x, _position.Y));
    [UnscopedRef] public ref PathBuilder HorizontalLineRelative(float xOffset) => ref LineTo(new(_position.X + xOffset, _position.Y));

    [UnscopedRef] public ref PathBuilder VerticalLineTo(float y) => ref LineTo(new(_position.X, y));
    [UnscopedRef] public ref PathBuilder VerticalLineRelative(float yOffset) => ref LineTo(new(_position.X, _position.Y + yOffset));
    
    [UnscopedRef]
    public ref PathBuilder BezierTo(Vector2 control, Vector2 destination) {
        _operations.Add(new() {
            Type = PathOperationType.QuadraticBezier,
            QuadraticBezier = new(control, destination),
        });
        _position = destination;

        return ref this;
    }
    [UnscopedRef] public ref PathBuilder BezierRelative(Vector2 controlOffset, Vector2 destinationOffset) => ref BezierTo(_position + controlOffset, _position + destinationOffset);

    [UnscopedRef]
    public ref PathBuilder BezierTo(Vector2 startControl, Vector2 endControl, Vector2 destination) {
        _operations.Add(new() {
            Type = PathOperationType.CubicBezier,
            CubicBezier = new(startControl, endControl, destination),
        });
        _position = destination;

        return ref this;
    }
    [UnscopedRef] public ref PathBuilder BezierRelative(Vector2 startControlOffset, Vector2 endControlOffset, Vector2 destinationOffset) => ref BezierTo(_position + startControlOffset, _position + endControlOffset, _position + destinationOffset);

    [UnscopedRef]
    public ref PathBuilder CloseSubpath(PathLooping looping = PathLooping.None, PathCapType capType = PathCapType.Butt) {
        _operations.Add(new() {
            Type = PathOperationType.Close,
            Close = new() {
                CapType = capType is PathCapType.Butt or PathCapType.Round or PathCapType.Square ? capType : PathCapType.Butt,
                Loop = looping,
            },
        });

        return ref this;
    }
    
    [UnscopedRef]
    public ref PathBuilder End() {
        PathBuilding.Build(_builder, CollectionsMarshal.AsSpan(_operations), 1, Color32.White, _config, _writer, _indexFormat);
        _operations.Clear();
        
        return ref this;
    }
}