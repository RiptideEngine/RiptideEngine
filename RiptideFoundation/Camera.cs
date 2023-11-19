namespace RiptideFoundation;

[EnumExtension]
public enum CameraProjection {
    Perspective,
    Orthographics,
}

[EntityComponent("00d590bc-dd99-417d-97ac-ae6cc5eaba47")]
public sealed class Camera : Component {
    public Color ClearColor { get; set; }
    [JsonInclude, JsonPropertyName("ProjectionType")] private CameraProjection _projectionType;
    [JsonPropertyName("FOV")] public float PerspectiveFOV { get; set; }
    [JsonPropertyName("OrthoSize")] public float OrthographicSize { get; set; }
    public Rectangle2D Viewport { get; set; }
    public Bound2D ScissorRect { get; set; }
    public float NearPlane { get; set; }
    public float FarPlane { get; set; }

    [JsonIgnore] public float AspectRatio { get; set; }

    [JsonIgnore]
    public CameraProjection ProjectionType {
        get => _projectionType;
        set => _projectionType = value.IsDefined() ? value : CameraProjection.Perspective;
    }

    [JsonIgnore]
    public Matrix4x4 ViewMatrix {
        get {
            return Matrix4x4.CreateLookToLeftHanded(Entity.GlobalPosition, Vector3.Transform(Vector3.UnitZ, Entity.GlobalRotation), Vector3.Transform(Vector3.UnitY, Entity.GlobalRotation));
        }
    }

    [JsonIgnore]
    public Matrix4x4 ProjectionMatrix {
        get {
            return _projectionType switch {
                CameraProjection.Orthographics => Matrix4x4.CreateOrthographicLeftHanded(AspectRatio * OrthographicSize, OrthographicSize, NearPlane, FarPlane),
                CameraProjection.Perspective or _ => Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(PerspectiveFOV, AspectRatio, NearPlane, FarPlane),
            };
        }
    }

    [JsonConstructor]
    private Camera() {
        ClearColor = new(0, 0.2f, 0.4f, 1);
        _projectionType = CameraProjection.Perspective;
        PerspectiveFOV = 70f * float.Pi / 180f;
        OrthographicSize = 10;
        NearPlane = 0.001f;
        FarPlane = 1000f;
        Viewport = new(Vector2.Zero, Vector2.One);
        ScissorRect = new(0, 0, 1, 1);

        AspectRatio = 16f / 9f;

        // var ss = Screen.Size;
        // AspectRatio = ss.X / ss.Y;
    }
}