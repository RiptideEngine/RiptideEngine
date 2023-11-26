namespace Riptide.UserInterface;

public readonly struct Vertex(Vector2 position, Vector2 uv, Color32 color) {
    public readonly Vector2 Position = position;
    public readonly Vector2 TexCoord = uv;
    public readonly Color32 Color = color;
}