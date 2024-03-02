namespace RiptideEditorV2.UI;

public sealed unsafe class Button : VisualElement {
    private Vector4 _borderRadii;
    public Vector4 BorderRadii {
        get => _borderRadii;
        set {
            value = Vector4.Max(value, Vector4.Zero);
            if (Vector4.DistanceSquared(value, _borderRadii) <= 0.001f) return;

            _borderRadii = value;
            InvalidateGraphics();
        }
    }
    
    public event Action? OnClick;

    public TextElement Text { get; private set; }

    public Button() {
        // Text = new() {
        //     Size = 18,
        //     Font = EditorApplication._ui.DefaultFont,
        //     Text = string.Empty,
        //     Name = "Text",
        //     Layout = {
        //         X = 0,
        //         Y = 0,
        //         Height = 100,
        //         Width = 100,
        //     },
        // };
        // Text.SetParent(this);
    }
    
    public override void BindMaterialProperties(MaterialProperties properties) {
        properties.SetTexture("_MainTexture", Graphics.WhiteTexture);
    }

    public override int CalculateMaterialBatchingHash() => 0;

    public override void GenerateMesh(MeshBuilder builder, Matrix3x2 transformation) {
        (float x, float y, float w, float h) = ResolvedLayout.Rectangle;

        Vector4 borderRadii = Vector4.Min(_borderRadii, new Vector4(w, h, w, h) / 2f);

        var pathBuilder = new PathBuilder(builder, Writer, IndexFormat.UInt16);

        // TODO: Calculate the smooth resolution based on button size.

        pathBuilder.Begin(PathBuildingConfiguration.Default with {
            BezierCurveResolution = 32,
        });
        {
            pathBuilder.SetColor(Color32.White).SetThickness(10);

            pathBuilder.MoveTo(400, 500);
            pathBuilder.BezierRelative(new(50, -200), new(450, -200), new(500, 0));
            pathBuilder.CloseSubpath(PathLooping.None);

            // pathBuilder.SetColor(Color32.White).SetThickness(15);
            //
            // var start = Vector2.Transform(new(borderRadii.X, 0), transformation);
            // pathBuilder.MoveTo(start);
            //
            // float radius = borderRadii.Y;
            // if (radius <= 0.01f) {
            //     pathBuilder.LineTo(Vector2.Transform(new(w, 0), transformation));
            // } else {
            //     pathBuilder.LineTo(Vector2.Transform(new(w - radius, 0), transformation));
            //     pathBuilder.LineTo(Vector2.Transform(new(w, radius), transformation));
            // }
            //
            // radius = borderRadii.Z;
            // if (radius <= 0.01f) {
            //     pathBuilder.LineTo(Vector2.Transform(new(w, h), transformation));
            // } else {
            //     pathBuilder.LineTo(Vector2.Transform(new(w, h - radius), transformation));
            //     pathBuilder.LineTo(Vector2.Transform(new(w - radius, h), transformation));
            // }
            //
            // radius = borderRadii.W;
            // if (radius <= 0.01f) {
            //     pathBuilder.LineTo(Vector2.Transform(new(0, h), transformation));
            // } else {
            //     pathBuilder.LineTo(Vector2.Transform(new(radius, h), transformation));
            //     pathBuilder.LineTo(Vector2.Transform(new(0, h - radius), transformation));
            // }
            //
            // radius = borderRadii.X;
            // if (radius <= 0.01f) {
            //     pathBuilder.LineTo(transformation.Translation);
            // } else {
            //     pathBuilder.LineTo(Vector2.Transform(new(0, radius), transformation));
            //     pathBuilder.LineTo(start);
            // }
            //
            // pathBuilder.CloseSubpath(looping: false);
        }
        pathBuilder.End();
        
        static void Writer(MeshBuilder builder, PathBuilding.Vertex vertex) {
            builder.WriteVertex(new Vertex(vertex.Position, Vector2.Zero, vertex.Color));
        }
    }
}