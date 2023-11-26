using RiptideRendering;

namespace Riptide.UserInterface;

public sealed partial class RenderingList {
    private List<Vertex> _vertices;
    private List<ushort> _indices;
    
    private List<RenderingCommand> _commands;
    
    private Stack<Bound2DInt> _scissorRects;
    private Stack<(ResourceSignature Signature, PipelineState State)> _pipelines;

    public int VertexCount => _vertices.Count;
    public int IndexCount => _indices.Count;

    internal ReadOnlySpan<Vertex> AllocatedVertices => CollectionsMarshal.AsSpan(_vertices);
    internal ReadOnlySpan<ushort> AllocatedIndices => CollectionsMarshal.AsSpan(_indices);

    internal ReadOnlySpan<RenderingCommand> RenderingCommands => CollectionsMarshal.AsSpan(_commands);
    
    public uint CurrentVertexOffset { get; private set; }

    internal RenderingList(ResourceSignature defaultResourceSignature, PipelineState defaultPipelineState) {
        _vertices = [];
        _indices = [];

        _commands = [];

        _scissorRects = [];
        _pipelines = [];
    }

    internal void PushDefaultConfigurations(Bound2DInt scissorRect, ResourceSignature signature, PipelineState state) {
        _scissorRects.Push(scissorRect);
        _pipelines.Push((signature, state));
    }

    public void IncrementVertexOffset(int count) {
        CurrentVertexOffset += (uint)count;
    }

    internal void Reset() {
        _vertices.Clear();
        _indices.Clear();
        _commands.Clear();
        _scissorRects.Clear();
        _pipelines.Clear();

        CurrentVertexOffset = 0;
    }
    
    public Span<Vertex> PreserveVertices(int count) {
        int vc = _vertices.Count;
        CollectionsMarshal.SetCount(_vertices, _vertices.Count + count);
        
        return CollectionsMarshal.AsSpan(_vertices)[vc..];
    }
    public Span<ushort> PreserveIndices(int count) {
        int ic = _indices.Count;
        CollectionsMarshal.SetCount(_indices, _indices.Count + count);

        CollectionsMarshal.AsSpan(_commands)[^1].IndexCount += (uint)count;
        
        return CollectionsMarshal.AsSpan(_indices)[ic..];
    }

    public void PushScissorRect(Bound2DInt scissorRect, bool intersect = true) {
        if (_commands.Count == 0) {
            _scissorRects.Push(scissorRect);
            AddRenderingCommand();
        } else {
            if (intersect) {
                scissorRect = Bound2DInt.GetIntersect(scissorRect, _commands[^1].ScissorRect);
            }
            
            _scissorRects.Push(scissorRect);
            AddRenderingCommand();
        }
    }

    public void PopClipRect() {
        if (_scissorRects.Count == 1) return;
        
        _scissorRects.Pop();
        AddRenderingCommand();
    }

    public void PushMaterial(ResourceSignature? overrideSignature, PipelineState pipelineState) {
        
    }

    public void PopMaterial() {
        if (_pipelines.Count == 1) return;

        _pipelines.Pop();
        AddRenderingCommand();
    }
    
    private void AddRenderingCommand() {
        if (!_scissorRects.TryPeek(out var scissorRect) || !_pipelines.TryPeek(out var pipeline)) return;

        CurrentVertexOffset = 0;
        
        RenderingCommand cmd = new() {
            ScissorRect = scissorRect,
            OverrideSignature = pipeline.Signature,
            PipelineState = pipeline.State,
            IndexCount = 0,
            VertexOffset = (uint)_vertices.Count,
            IndexOffset = (uint)_indices.Count,
        };
        
        _commands.Add(cmd);
    }
}