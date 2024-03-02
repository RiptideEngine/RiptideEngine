using RiptideEngine.Core.Allocation;
using RiptideFoundation.Rendering;

namespace RiptideFoundation.Helpers;

public sealed unsafe class MeshBuilder : IDisposable {
    private const int DefaultIndexArrayPoolSize = 256;

    private VertexDescriptor[] _vdescs;
    private SegmentedMemoryBlock?[] _segments;

    private int _channelIndex = -1;

    private SegmentedMemoryBlock _indices = new(DefaultIndexArrayPoolSize);

    public int WrittenIndexByteLength => _indices.Length;

    public MeshBuilder(ReadOnlySpan<VertexDescriptor> descriptors) {
        Mesh.ValidateVertexDescriptors(descriptors);

        _vdescs = descriptors.ToArray();
        _segments = new SegmentedMemoryBlock?[descriptors.Length];
    }
    
    public MeshBuilder SetVertexChannel(uint channel) {
        if (channel >= Mesh.MaximumVertexChannels) throw new ArgumentException("Channel value surpassed Mesh's maximum amount of channels.");

        for (int i = 0; i < _segments.Length; i++) {
            if (_vdescs[i].Channel == channel) {
                _channelIndex = i;
                return this;
            }
        }

        throw new ArgumentException("Cannot switch vertex channel because it is not included in vertex descriptors.");
    }

    public MeshBuilder WriteVertex<T>(T vertex) where T : unmanaged {
        return WriteVertices(&vertex, 1);
    }

    public MeshBuilder WriteVertices<T>(ReadOnlySpan<T> vertices) where T : unmanaged {
        if (vertices.IsEmpty) return this;

        fixed (T* pVertices = vertices) {
            return WriteVertices(pVertices, vertices.Length);
        }
    }

    public MeshBuilder WriteVertices<T>(T* vertices, int count) where T : unmanaged {
        if (_channelIndex == -1) throw new InvalidOperationException("Channel index must be set first before writing vertex.");
        
        var segment = _segments[_channelIndex] ?? new SegmentedMemoryBlock(sizeof(T) * count);
        _segments[_channelIndex] = segment.Write((byte*)vertices, sizeof(T) * count);
        return this;
    }
    
    public MeshBuilder WriteIndex(ushort index) {
        _indices.Write((byte*)&index, sizeof(ushort));
        return this;
    }

    public MeshBuilder WriteIndex(uint index) {
            _indices.Write((byte*)&index, sizeof(uint));
            return this;
    }
    
    public MeshBuilder WriteIndices(ReadOnlySpan<ushort> indices) {
        if (indices.IsEmpty) return this;

        fixed (ushort* pIndices = indices) {
            _indices.Write((byte*)pIndices, sizeof(ushort) * indices.Length);
        }

        return this;
    }
    
    public MeshBuilder WriteIndices(ReadOnlySpan<uint> indices) {
        if (indices.IsEmpty) return this;

        fixed (uint* pIndices = indices) {
            _indices.Write((byte*)pIndices, sizeof(uint) * indices.Length);
        }

        return this;
    }

    public int GetWrittenVertexCount() => GetWrittenVertexByteLength() / (int)_vdescs[_channelIndex].Stride;
    public int GetWrittenVertexCount(uint channel) => GetWrittenVertexByteLength(channel) / (int)_vdescs[_channelIndex].Stride;
    public int GetLargestWrittenVertexCount() {
        uint value = 0;

        foreach ((var desc, var segment) in _vdescs.Zip(_segments)) {
            if (segment == null) continue;

            value = uint.Max(value, (uint)segment.Length / desc.Stride);
        }

        return (int)value;
    }    
    
    public int GetWrittenVertexByteLength() => _segments[_channelIndex] is { } segment ? segment.Length : 0;
    public int GetWrittenVertexByteLength(uint channel) {
        for (int i = 0; i < _vdescs.Length; i++) {
            if (_vdescs[i].Channel == channel) {
                return _segments[i] is { } block ? block.Length : 0;
            }
        }

        return 0;
    }
    public int GetLargestWrittenVertexByteLength() {
        int value = 0;

        foreach ((var desc, var segment) in _vdescs.Zip(_segments)) {
            if (segment == null) continue;

            value = int.Max(value, segment.Length);
        }

        return value;
    }

    public void Clear() {
        _indices.Clear();
        
        foreach (var segment in _segments) {
            segment?.Clear();
        }
    }
    
    public void Commit(CommandList cmdList, Mesh receiver, bool forceFit) {
        if (!Mesh.CheckLayoutCompatibility(receiver.VertexDescriptors, _vdescs)) {
            throw new ArgumentException("Mesh receiver has uncompatible layout.");
        }
        
        uint largestNumVertices = GetLargestVertexCount();

        if (forceFit) {
            receiver.AllocateVertexBuffers(largestNumVertices);
            receiver.AllocateIndexBuffer((uint)_indices.Length);
        } else {
            if (receiver.VertexCount < largestNumVertices) {
                receiver.AllocateVertexBuffers(largestNumVertices);
            }
            
            if (receiver.IndexBuffer == null || receiver.IndexBuffer.Description.Width < (uint)_indices.Length) {
                receiver.AllocateIndexBuffer((uint)_indices.Length);
            }
        }
        
        CommitToMesh(cmdList, receiver);
    }

    public Mesh Commit(CommandList cmdList) {
        uint largestNumVertices = GetLargestVertexCount();
        
        if (largestNumVertices == 0 && _indices.Length == 0) return new();
        
        Mesh mesh = new();
        mesh.AllocateVertexBuffers(largestNumVertices, _vdescs);
        mesh.AllocateIndexBuffer((uint)_indices.Length);

        CommitToMesh(cmdList, mesh);
        
        return mesh;
    }

    private void CommitToMesh(CommandList cmdList, Mesh mesh) {
        foreach (ref readonly var desc in _vdescs.AsSpan()) {
            cmdList.TranslateResourceState(mesh.GetVertexBuffer(desc.Channel)!.UnderlyingBuffer, ResourceTranslateStates.CopyDestination);
        }
        
        foreach ((var desc, var segment) in _vdescs.Zip(_segments)) {
            if (segment == null) continue;
        
            cmdList.UpdateBuffer(mesh.GetVertexBuffer(desc.Channel)!.UnderlyingBuffer, BufferUpdate, segment);
        }
        
        foreach (ref readonly var desc in _vdescs.AsSpan()) {
            cmdList.TranslateResourceState(mesh.GetVertexBuffer(desc.Channel)!.UnderlyingBuffer, ResourceTranslateStates.ShaderResource);
        }

        if (mesh.IndexBuffer != null) {
            cmdList.TranslateResourceState(mesh.IndexBuffer, ResourceTranslateStates.CopyDestination);
            cmdList.UpdateBuffer(mesh.IndexBuffer, BufferUpdate, _indices);
            cmdList.TranslateResourceState(mesh.IndexBuffer, ResourceTranslateStates.IndexBuffer);
        }
    }
    
    private static void BufferUpdate(Span<byte> destination, SegmentedMemoryBlock block) {
        block.Read(destination);
    }

    private uint GetLargestVertexCount() {
        uint largestNumVertices = 0;
        foreach ((var vdesc, var segment) in _vdescs.Zip(_segments)) {
            if (segment == null) continue;

            largestNumVertices = uint.Max(largestNumVertices, (uint)segment.Length / vdesc.Stride);
        }

        return largestNumVertices;
    }

    public void Dispose() {
        foreach (var segment in _segments) {
            segment?.Dispose();
        }
        _indices.Dispose();

        _segments = [];
        _vdescs = [];
        _indices = null!;
    }
}