namespace RiptideFoundation;

public sealed class VertexBatch : RiptideRcObject {
    private Mesh _mesh;
    private CommandList? _cmdList;

    public override string? Name {
        get => base.Name;
        set {
            if (Interlocked.Read(ref _refcount) == 0) return;
            
            base.Name = value;
            _mesh.Name = $"{value}.Mesh";
        }
    }

    public VertexBatch(uint numVertices, ReadOnlySpan<VertexDescriptor> vertexDescriptors, uint numIndices, IndexFormat format) {
        _mesh = new() {
            Name = $"<Unnamed {nameof(VertexBatch)}>.Mesh",
        };
        
        _mesh.AllocateVertexBuffers(numVertices, vertexDescriptors);
        _mesh.AllocateIndexBuffer(numIndices, format);

        _refcount = 1;
    }

    public void Begin(CommandList cmdList) {
        if (_cmdList != null) throw new InvalidOperationException("An command list is currently being used. Make sure that you are not calling Begin in another Begin block.");

        cmdList.IncrementReference();
        _cmdList = cmdList;
    }

    public void SetChannel(int channel) {
        
    }

    public void End() {
        if (_cmdList == null) throw new InvalidOperationException("No command list is currently being used. Are you calling End without Begin?");

        _cmdList.DecrementReference();
        _cmdList = null!;
    }

    public void Flush() {
        
    }

    protected override void Dispose() {
        _mesh.DecrementReference();
    }
}