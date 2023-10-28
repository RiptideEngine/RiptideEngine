namespace RiptideFoundation;

internal enum MeshInternalFlags {
    None = 0,

    IsResource = 1 << 0,
}
public readonly record struct VertexDescriptor(uint Stride, uint Channel);
public readonly record struct SubmeshInformation(uint StartIndex, uint IndexCount, MeshBoundaryShape Shape);

public sealed class Mesh : RiptideRcObject, IResourceAsset {
    private GpuBuffer? _indexBuffer;
    private GpuBuffer[] _vertexBuffers;

    private MeshInternalFlags _internalFlags;
    private VertexDescriptor[] _descriptors;
    private SubmeshInformation[] _submeshes;

    public uint VertexCount { get; private set; }
    public uint IndexCount { get; private set; }
    public IndexFormat IndexFormat { get; private set; }
    public ReadOnlySpan<VertexDescriptor> VertexDescriptors => _descriptors;
    public ReadOnlySpan<SubmeshInformation> Submeshes => _submeshes;
    public bool IsResourceAsset => _internalFlags.HasFlag(MeshInternalFlags.IsResource);

    public GpuBuffer? IndexBuffer => _indexBuffer;

    public Mesh() {
        RuntimeFoundation.AssertInitialized();

        _descriptors = [];
        _vertexBuffers = [];
        _submeshes = [];
    }

    public void AllocateVertexBuffer(uint vertexCount, ReadOnlySpan<VertexDescriptor> descriptors) {
        if (vertexCount == 0 || descriptors.IsEmpty) {
            VertexCount = vertexCount;
            _descriptors = [];

            foreach (var buffer in _vertexBuffers) buffer.DecrementReference();
            _vertexBuffers = [];

            return;
        }

        ValidateVertexDescriptors(descriptors);

        var factory = Graphics.RenderingContext.Factory;
        GpuBuffer?[] outputBuffers = new GpuBuffer?[descriptors.Length];

        try {
            int index = 0;
            foreach (ref readonly var descriptor in descriptors) {
                outputBuffers[index] = factory.CreateBuffer(new BufferDescriptor {
                    Size = descriptor.Stride * vertexCount,
                    Flags = BufferFlags.None,
                });

                index++;
            }
        } catch {
            foreach (var buffer in outputBuffers) {
                buffer?.DecrementReference();
            }

            throw;
        }

        _descriptors = descriptors.ToArray();
        foreach (var buffer in _vertexBuffers) buffer.DecrementReference();
        _vertexBuffers = outputBuffers!;
    }
    public void AllocateIndexBuffer(uint indexCount, IndexFormat format) {
        if (!format.IsDefined()) throw new ArgumentException("Undefined index format.", nameof(format));

        if (indexCount == 0) {
            _indexBuffer?.DecrementReference();
            _indexBuffer = null;

            IndexCount = 0;
            IndexFormat = IndexFormat.UInt16;
            return;
        }

        _indexBuffer = Graphics.RenderingContext.Factory.CreateBuffer(new BufferDescriptor {
            Size = indexCount * (2U << (int)format),
            Flags = BufferFlags.None,
        });

        IndexCount = indexCount;
        IndexFormat = format;
    }
    public void SetSubmeshes(ReadOnlySpan<SubmeshInformation> submeshes) {
        _submeshes = submeshes.ToArray();
    }

    public bool TryGetVertexBuffer(uint channel, [NotNullWhen(true)] out GpuBuffer? output) {
        int index = 0;
        foreach (ref readonly var descriptor in _descriptors.AsSpan()) {
            if (descriptor.Channel == channel) {
                output = _vertexBuffers[index];
                return true;
            }

            index++;
        }

        output = null;
        return false;
    }
    public GpuBuffer? GetVertexBuffer(uint channel) {
        TryGetVertexBuffer(channel, out var output);
        return output;
    }

    public IEnumerable<(GpuBuffer Buffer, VertexDescriptor Descriptor)> EnumerateVertexBuffers() {
        int idx = 0;
        foreach (var descriptor in _descriptors) {
            yield return (_vertexBuffers[idx], _descriptors[idx]);

            idx++;
        }
    }

    public bool CanInstantiate<T>() => false;
    public bool CanInstantiate(Type type) => false;

    public bool TryInstantiate<T>([NotNullWhen(true)] out T? output) {
        output = default;
        return false;
    }
    public bool TryInstantiate(Type outputType, [NotNullWhen(true)] out object? output) {
        output = default;
        return false;
    }

    protected override void Dispose() {
        foreach (var buffer in _vertexBuffers) buffer.DecrementReference();
        _vertexBuffers = [];

        _indexBuffer?.DecrementReference();

        _indexBuffer = null!;
    }

    private static void ValidateVertexDescriptors(ReadOnlySpan<VertexDescriptor> descriptors) {
        byte duplicateChannelFlags = 0;

        int i = 0;
        foreach (ref readonly var descriptor in descriptors) {
            if (descriptor.Channel >= 8) throw new ArgumentException($"Vertex descriptor index {i} is using channel {descriptor.Channel}. Only channel 0 to 7 (inclusive) is allowed.");
            if ((duplicateChannelFlags & (1U << (int)descriptor.Channel)) != 0) throw new ArgumentException($"Multiple vertex descriptors of channel '{descriptor.Channel}' detected.", nameof(descriptors));

            i++;
            duplicateChannelFlags |= (byte)(1 << (int)descriptor.Channel);
        }
    }
}