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

        float t = (float)(EditorApplication.ElapsedTime * 0.25f % 1);

        pathBuilder.Begin();
        {
            pathBuilder.SetColor(Color32.Red).SetThickness(10)
                       .MoveTo(new(200, 500))
                       .VerticalLineRelative(100)
                       .HorizontalLineRelative(-100)
                       .CloseSubpath(PathCapType.Round);
        }
        pathBuilder.End();
        
        static void Writer(MeshBuilder builder, PathBuilding.Vertex vertex) {
            builder.WriteVertex(new Vertex(vertex.Position, Vector2.Zero, vertex.Color));
        }

        static int CalculateSegmentResolution(float radius) {
            float length = float.Pi / 2 * radius;
            return (int)float.Ceiling(float.Sinh(float.Pow(length, 0.2f)) + 1);
        }
    }
}