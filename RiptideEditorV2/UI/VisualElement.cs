using RiptideFoundation.Rendering;

namespace RiptideEditorV2.UI;

public unsafe class VisualElement : InterfaceElement {
    private MaterialPipeline? _pipeline;
    public MaterialPipeline? Pipeline {
        get => _pipeline;
        set {
            if (Document == null || ReferenceEquals(_pipeline, value)) return;

            _pipeline = value ?? Document.Renderer.GetDefaultPipeline(GetType());
            _pipeline.IncrementReference();
            
            if (value != null) {
                Document.Renderer.MarkElementDirty(this);
            }
        }
    }

    public void InvalidateGraphics() {
        Document?.Renderer.MarkElementDirty(this);
    }
    
    public sealed override void Invalidate(InvalidationFlags flags = InvalidationFlags.All) {
        base.Invalidate(flags);

        if (flags.HasFlag(InvalidationFlags.Graphics)) {
            InvalidateGraphics();
        }
    }

    public virtual void BindMaterialProperties(MaterialProperties properties) { }

    public virtual int CalculateMaterialBatchingHash() => 0;

    public virtual void GenerateMesh(MeshBuilder builder, Matrix3x2 transformation) {
        var vcount = builder.GetWrittenVertexCount(0) / sizeof(Vertex);

        var size = ResolvedLayout.Rectangle.Size;
        
        builder.WriteVertex(new Vertex(transformation.Translation, Vector2.Zero, Color32.White));
        builder.WriteVertex(new Vertex(Vector2.Transform(size with {
            Y = 0,
        }, transformation), Vector2.UnitX, Color32.White));
        builder.WriteVertex(new Vertex(Vector2.Transform(size, transformation), Vector2.One, Color32.White));
        builder.WriteVertex(new Vertex(Vector2.Transform(size with {
            X = 0,
        }, transformation), Vector2.UnitY, Color32.White));

        builder.WriteIndices(stackalloc ushort[] {
            (ushort)(vcount + 0),
            (ushort)(vcount + 1),
            (ushort)(vcount + 2),
            (ushort)(vcount + 2),
            (ushort)(vcount + 3),
            (ushort)(vcount + 0),
        });
    }

    protected override void DisposeImpl(bool disposing) {
        _pipeline?.DecrementReference();
        _pipeline = null!;
    }

    ~VisualElement() {
        DisposeImpl(false);
    }
}