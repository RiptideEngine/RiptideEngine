using RiptideFoundation.Rendering;

namespace RiptideEditorV2.UI;

public sealed unsafe class ImageElement : VisualElement {
    private Sprite? _sprite;
    public Sprite? Sprite {
        get => _sprite;
        set {
            if (ReferenceEquals(_sprite, value)) return;

            _sprite = value;

            Document?.Renderer.MarkElementDirty(this);
        }
    }

    public ImageElement() {
    }

    public override int CalculateMaterialBatchingHash() {
        return _sprite?.GetHashCode() ?? 0;
    }

    public override void BindMaterialProperties(MaterialProperties properties) {
        properties.SetTexture("_MainTexture", _sprite.Texture);
    }

    public override void GenerateMesh(MeshBuilder builder, Matrix3x2 transformation) {
        if (_sprite == null) return;

        var vcount = builder.GetWrittenVertexCount(0) / sizeof(Vertex);

        var size = ResolvedLayout.Rectangle.Size;
        (var uvMin, var uvMax) = _sprite.Boundary;
        
        builder.WriteVertex(new Vertex(transformation.Translation, uvMin, Color32.White));
        builder.WriteVertex(new Vertex(Vector2.Transform(size with { Y = 0 }, transformation), uvMin with { X = uvMax.X }, Color32.White));
        builder.WriteVertex(new Vertex(Vector2.Transform(size, transformation), uvMax, Color32.White));
        builder.WriteVertex(new Vertex(Vector2.Transform(size with { X = 0 }, transformation), uvMin with { Y = uvMax.Y }, Color32.White));

        builder.WriteIndices(stackalloc ushort[] {
            (ushort)(vcount + 0),
            (ushort)(vcount + 1),
            (ushort)(vcount + 2),
            (ushort)(vcount + 2),
            (ushort)(vcount + 3),
            (ushort)(vcount + 0),
        });
    }
}