namespace RiptideFoundation.Rendering;

public sealed class Sprite : RenderingObject {
    public Texture2D Texture { get; private set; }
    public Bound2D Boundary { get; private set; }
    public Vector2 Pivot { get; private set; }
    public float PixelsPerUnit { get; private set; }
    
    public Sprite(Texture2D texture, Bound2D boundary, Vector2 pivot, float pixelsPerUnit) {
        Texture = texture;
        Boundary = boundary;
        Pivot = pivot;
        PixelsPerUnit = pixelsPerUnit;

        Texture.IncrementReference();

        _refcount = 1;
    }

    protected override void Dispose() {
        Texture.DecrementReference();
        Texture = null!;
        
        Boundary = Bound2D.Zero;
    }
}