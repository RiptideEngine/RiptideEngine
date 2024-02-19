using RiptideFoundation.Helpers;
using RiptideFoundation.Rendering;

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
        Text = new() {
            Size = 18,
            Font = EditorApplication._ui.DefaultFont,
            Text = string.Empty,
            Name = "Text",
            Layout = {
                X = 0,
                Y = 0,
                Height = 100,
                Width = 100,
            },
        };
        Text.SetParent(this);
    }
    
    public override void BindMaterialProperties(MaterialProperties properties) {
        properties.SetTexture("_MainTexture", Graphics.WhiteTexture);
    }

    public override int CalculateMaterialBatchingHash() => 0;

    public override void GenerateMesh(MeshBuilder builder, Matrix3x2 transformation) {
        (float x, float y, float w, float h) = ResolvedLayout.Rectangle;

        Vector4 borderRadii = Vector4.Min(_borderRadii, new Vector4(w, h, w, h) / 2f);

        var pathBuilder = new PathBuilder(builder, Writer, IndexFormat.UInt16);

        pathBuilder.Begin();
        {
            pathBuilder.SetColor(new(114)).SetThickness(2);

            for (int i = 1; i <= 30; i++) {
                var xPos = 50 + 60 * i;
                
                pathBuilder.MoveTo(new(xPos, 65));
                pathBuilder.VerticalLineTo(EditorApplication.WindowSize.Y - 50);
            }

            for (int i = 1; i <= 14; i++) {
                var yPos = EditorApplication.WindowSize.Y - 50 - 60 * i;
                
                pathBuilder.MoveTo(new(50, yPos));
                pathBuilder.HorizontalLineTo(EditorApplication.WindowSize.X - 65);
            }

            pathBuilder.SetColor(Color32.Cyan).SetThickness(8);
            pathBuilder.MoveTo(new(52, 740));

            for (int i = 0; i < 260; i++) {
                pathBuilder.LineTo(new(52 + i * 7, 500 + float.Cos(i / 180f * float.Pi * 20) * 180));
            }
            
            pathBuilder.SetColor(Color32.LightGray).SetThickness(5);

            pathBuilder.MoveTo(new(50, 50));
            pathBuilder.LineTo(new(50, EditorApplication.WindowSize.Y - 50));
            pathBuilder.LineTo(new(EditorApplication.WindowSize.X - 50, EditorApplication.WindowSize.Y - 50));
            pathBuilder.CloseSubpath();

            // pathBuilder.MoveTo(Vector2.Transform(new(borderRadii.X, 0), transformation));
            //            
            // // Draw top right corner.
            // {
            //     float radius = borderRadii.Y;
            //     int resolution = CalculateSegmentResolution(radius);
            //     
            //     Vector2 origin = new(w - radius, radius);
            //     float step = float.Pi / 2 / resolution;
            //
            //     for (int i = 0; i <= resolution; i++) {
            //         var radian = float.Tau - float.Pi / 2 + step * i;
            //         (float sin, float cos) = float.SinCos(radian);
            //         
            //         pathBuilder.LineTo(Vector2.Transform(origin + new Vector2(cos, sin) * radius, transformation));
            //     }
            // }
            //
            // // Draw bottom right corner.
            // {
            //     float radius = borderRadii.Z;
            //     int resolution = CalculateSegmentResolution(radius);
            //
            //     Vector2 origin = new(w - radius, h - radius);
            //     float step = float.Pi / 2 / resolution;
            //
            //     for (int i = 0; i <= resolution; i++) {
            //         var radian = step * i;
            //         (float sin, float cos) = float.SinCos(radian);
            //         
            //         pathBuilder.LineTo(Vector2.Transform(origin + new Vector2(cos, sin) * radius, transformation));
            //     }
            // }
            //
            // // Draw bottom left corner.
            // {
            //     float radius = borderRadii.W;
            //     int resolution = CalculateSegmentResolution(radius);
            //
            //     Vector2 origin = new(radius, h - radius);
            //     float step = float.Pi / 2 / resolution;
            //
            //     for (int i = 0; i <= resolution; i++) {
            //         var radian = float.Pi / 2 + step * i;
            //         (float sin, float cos) = float.SinCos(radian);
            //     
            //         pathBuilder.LineTo(Vector2.Transform(origin + new Vector2(cos, sin) * radius, transformation));
            //     }
            // }
            //            
            // // Draw top left corner.
            // {
            //     float radius = borderRadii.X;
            //     int resolution = CalculateSegmentResolution(radius);
            //
            //     Vector2 origin = new(radius, radius);
            //     float step = float.Pi / 2 / resolution;
            //
            //     for (int i = 0; i <= resolution; i++) {
            //         var radian = float.Pi + step * i;
            //         (float sin, float cos) = float.SinCos(radian);
            //     
            //         pathBuilder.LineTo(Vector2.Transform(origin + new Vector2(cos, sin) * radius, transformation));
            //     }
            // }
        }
        pathBuilder.End();
        
        static void Writer(MeshBuilder builder, PathBuilder.Vertex vertex) {
            builder.WriteVertex(new Vertex(vertex.Position, Vector2.Zero, vertex.Color));
        }

        static int CalculateSegmentResolution(float radius) {
            float length = float.Pi / 2 * radius;
            return (int)float.Ceiling(float.Sinh(float.Pow(length, 0.2f)) + 1);
        }
    }
}