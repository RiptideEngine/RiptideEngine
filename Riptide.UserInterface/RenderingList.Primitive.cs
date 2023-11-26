using RiptideRendering;

namespace Riptide.UserInterface;

partial class RenderingList {
    public void DrawFilledTriangle(Vertex v1, Vertex v2, Vertex v3) {
        var voffset = CurrentVertexOffset;

        var vertices = PreserveVertices(3);
        var indices = PreserveIndices(3);

        vertices[0] = v1;
        vertices[1] = v2;
        vertices[2] = v3;

        indices[0] = (ushort)(voffset + 0);
        indices[1] = (ushort)(voffset + 1);
        indices[2] = (ushort)(voffset + 2);
        
        IncrementVertexOffset(3);
    }
    public void DrawFilledTriangle(Vector2 v1, Vector2 v2, Vector2 v3, Color32 color) {
        var voffset = CurrentVertexOffset;

        var vertices = PreserveVertices(3);
        var indices = PreserveIndices(3);

        vertices[0] = new(v1, Vector2.Zero, color);
        vertices[1] = new(v2, Vector2.Zero, color);
        vertices[2] = new(v3, Vector2.Zero, color);

        indices[0] = (ushort)(voffset + 0);
        indices[1] = (ushort)(voffset + 1);
        indices[2] = (ushort)(voffset + 2);
        
        IncrementVertexOffset(3);
    }

    public void DrawFilledTriangle(Vector2 v1, Color32 c1, Vector2 v2, Color32 c2, Vector2 v3, Color32 c3) {
        var voffset = CurrentVertexOffset;

        var vertices = PreserveVertices(3);
        var indices = PreserveIndices(3);

        vertices[0] = new(v1, Vector2.Zero, c1);
        vertices[1] = new(v2, Vector2.Zero, c2);
        vertices[2] = new(v3, Vector2.Zero, c3);

        indices[0] = (ushort)(voffset + 0);
        indices[1] = (ushort)(voffset + 1);
        indices[2] = (ushort)(voffset + 2);
        
        IncrementVertexOffset(3);
    }
    public void DrawFilledRect(Rectangle2D rect, Color32 color) {
        var voffset = CurrentVertexOffset;
        
        var vertices = PreserveVertices(4);
        var indices = PreserveIndices(6);

        var max = rect.Position + rect.Size;

        vertices[0] = new(rect.Position, Vector2.Zero, color);
        vertices[1] = new(rect.Position with { X = max.X }, Vector2.Zero, color);
        vertices[2] = new(max, Vector2.Zero, color);
        vertices[3] = new(rect.Position with { Y = max.Y }, Vector2.Zero, color);

        indices[0] = (ushort)(voffset + 0);
        indices[1] = (ushort)(voffset + 1);
        indices[2] = (ushort)(voffset + 2);
        indices[3] = (ushort)(voffset + 2);
        indices[4] = (ushort)(voffset + 3);
        indices[5] = (ushort)(voffset + 0);
        
        IncrementVertexOffset(4);
    }
    public void DrawFilledRect(Rectangle2D rect, Color32 c1, Color32 c2, Color32 c3, Color32 c4) {
        var voffset = CurrentVertexOffset;
        
        var vertices = PreserveVertices(4);
        var indices = PreserveIndices(6);

        var max = rect.Position + rect.Size;

        vertices[0] = new(rect.Position, Vector2.Zero, c1);
        vertices[1] = new(rect.Position with { X = max.X }, Vector2.Zero, c2);
        vertices[2] = new(max, Vector2.Zero, c3);
        vertices[3] = new(rect.Position with { Y = max.Y }, Vector2.Zero, c4);

        indices[0] = (ushort)(voffset + 0);
        indices[1] = (ushort)(voffset + 1);
        indices[2] = (ushort)(voffset + 2);
        indices[3] = (ushort)(voffset + 2);
        indices[4] = (ushort)(voffset + 3);
        indices[5] = (ushort)(voffset + 0);
        
        IncrementVertexOffset(4);
    }

    public void DrawTexturedRect(Rectangle2D rect, Vector2 uvMin, Vector2 uvMax, Color32 color) {
        var voffset = CurrentVertexOffset;
        
        var vertices = PreserveVertices(4);
        var indices = PreserveIndices(6);

        var max = rect.Position + rect.Size;

        vertices[0] = new(rect.Position, uvMin, color);
        vertices[1] = new(rect.Position with { X = max.X }, uvMin with { X = uvMax.X }, color);
        vertices[2] = new(max, uvMax, color);
        vertices[3] = new(rect.Position with { Y = max.Y }, uvMin with { Y = uvMax.Y }, color);

        indices[0] = (ushort)(voffset + 0);
        indices[1] = (ushort)(voffset + 1);
        indices[2] = (ushort)(voffset + 2);
        indices[3] = (ushort)(voffset + 2);
        indices[4] = (ushort)(voffset + 3);
        indices[5] = (ushort)(voffset + 0);
        
        IncrementVertexOffset(4);
    }
    
    public void DrawFilledCircle(Circle circle, Color32 color, int subdivision) {
        if (subdivision <= 2 || circle.Radius <= 0) return;

        subdivision = Math.Min(subdivision, 64);

        var vc = CurrentVertexOffset;
        var vertices = PreserveVertices(subdivision + 1);
        var indices = PreserveIndices(subdivision * 3);
        
        Vector2 direction = new(circle.Radius / 2, 0);
        Matrix3x2 rotate = Matrix3x2.CreateRotation(float.Tau / subdivision);

        vertices[0] = new(circle.Position, Vector2.Zero, color);
        
        for (int i = 0; i < subdivision; i++) {
            Vector2 position = circle.Position + direction;

            vertices[i + 1] = new(position, Vector2.Zero, color);

            direction = Vector2.TransformNormal(direction, rotate);
        }

        for (int i = 0; i < subdivision - 1; i++) {
            indices[i * 3] = (ushort)vc;
            indices[i * 3 + 1] = (ushort)(vc + i + 1);
            indices[i * 3 + 2] = (ushort)(vc + i + 2);
        }
        
        indices[(subdivision - 1) * 3] = (ushort)vc;
        indices[(subdivision - 1) * 3 + 1] = (ushort)(vc + subdivision);
        indices[(subdivision - 1) * 3 + 2] = (ushort)(vc + 1);
        
        IncrementVertexOffset(subdivision + 1);
    }
    public void DrawQuad(Vertex v1, Vertex v2, Vertex v3, Vertex v4) {
        var voffset = CurrentVertexOffset;
        
        var vertices = PreserveVertices(4);
        var indices = PreserveIndices(6);

        vertices[0] = v1;
        vertices[1] = v2;
        vertices[2] = v3;
        vertices[3] = v4;

        indices[0] = (ushort)(voffset + 0);
        indices[1] = (ushort)(voffset + 1);
        indices[2] = (ushort)(voffset + 2);
        indices[3] = (ushort)(voffset + 2);
        indices[4] = (ushort)(voffset + 3);
        indices[5] = (ushort)(voffset + 0);
        
        IncrementVertexOffset(4);
    }
}