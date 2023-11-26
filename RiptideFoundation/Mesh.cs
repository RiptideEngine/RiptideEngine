namespace RiptideFoundation;

public readonly record struct VertexDescriptor(uint Stride, uint Channel);
public readonly record struct SubmeshInformation(uint StartIndex, uint IndexCount, MeshBoundaryShape Shape);

public sealed class Mesh : RiptideRcObject {
    private VertexDescriptor[] _vertexDescs;
    private VertexBuffer[] _vertexBuffers;
    private SubmeshInformation[] _submeshes;

    private uint _vertexCount, _indexCount;
    private IndexFormat _indexFormat;
    
    public GpuBuffer? IndexBuffer { get; private set; }

    public uint VertexCount => _vertexCount;
    public uint IndexCount => _indexCount;
    public IndexFormat IndexFormat => _indexFormat;
    public ReadOnlySpan<VertexDescriptor> VertexDescriptors => _vertexDescs;
    private ReadOnlySpan<SubmeshInformation> Submeshes => _submeshes;

    public override string? Name {
        get => base.Name;
        set {
            if (Interlocked.Read(ref _refcount) == 0) return;
            
            base.Name = value;

            if (IndexBuffer != null) IndexBuffer.Name = $"{value}.IndexBuffer";
            for (int i = 0; i < _vertexDescs.Length; i++) {
                (var resource, var view) = _vertexBuffers[i];

                resource.Name = $"{value}.VertexBuffer[{_vertexDescs[i].Channel}].Buffer";
                view.Name = $"{value}.VertexBuffer[{_vertexDescs[i].Channel}].View";
            }            
        }
    }

    public Mesh() {
        _vertexDescs = Array.Empty<VertexDescriptor>();
        _vertexBuffers = Array.Empty<VertexBuffer>();
        _submeshes = Array.Empty<SubmeshInformation>();

        _indexFormat = IndexFormat.UInt16;
        _refcount = 1;
    }

    public void AllocateVertexBuffers(uint numVertices, ReadOnlySpan<VertexDescriptor> descriptors) {
        if (numVertices == 0 || descriptors.IsEmpty) {
            _vertexCount = 0;
            _vertexDescs = Array.Empty<VertexDescriptor>();

            foreach ((var buffer, var view) in _vertexBuffers) {
                buffer.DecrementReference();
                view.DecrementReference();
            }
            _vertexBuffers = Array.Empty<VertexBuffer>();
            
            return;
        }
        
        ValidateVertexDescriptors(descriptors);

        var factory = Graphics.RenderingContext.Factory;
        VertexBuffer[] outputs = new VertexBuffer[descriptors.Length];
        int index = 0;
        
        try {
            ShaderResourceViewDescription srvdesc = new() {
                Dimension = ShaderResourceViewDimension.Buffer,
                Buffer = new() {
                    FirstElement = 0,
                    NumElements = numVertices,
                },
            };

            for (; index < descriptors.Length; index++) {
                var descriptor = descriptors[index];
                GpuBuffer? resource = null;
                ShaderResourceView? view = null;

                try {
                    resource = factory.CreateBuffer(new BufferDescription {
                        Width = numVertices * descriptor.Stride,
                    });
                    resource.Name = $"{Name}.VertexBuffer[{descriptor.Channel}].Buffer";
                    view = factory.CreateShaderResourceView(resource, srvdesc with {
                        Buffer = srvdesc.Buffer with { StructureSize = descriptor.Stride },
                    });
                    view.Name = $"{Name}.VertexBuffer[{descriptor.Channel}].View";
                } catch {
                    resource?.DecrementReference();
                    view?.DecrementReference();
                    throw;
                }

                outputs[index] = new(resource, view);
            }
        } catch {
            foreach ((var buffer, var view) in outputs) {
                buffer.DecrementReference();
                view.DecrementReference();
            }
            
            throw;
        }

        _vertexCount = numVertices;
        
        foreach ((var buffer, var view) in _vertexBuffers) {
            buffer.DecrementReference();
            view.DecrementReference();
        }
        _vertexBuffers = outputs;

        _vertexDescs = descriptors.ToArray();
    }

    public void AllocateIndexBuffer(uint numIndices, IndexFormat format) {
        if (!format.IsDefined()) throw new ArgumentException("Undefined index format.", nameof(format));

        if (numIndices == 0) {
            IndexBuffer?.DecrementReference();
            IndexBuffer = null;

            _indexCount = 0;
            return;
        }

        IndexBuffer = Graphics.RenderingContext.Factory.CreateBuffer(new() {
            Width = numIndices * (2U << (int)format),
        });
        IndexBuffer.Name = $"{Name}.IndexBuffer";

        _indexCount = numIndices;
        _indexFormat = format;
    }
    
    public void SetSubmeshes(ReadOnlySpan<SubmeshInformation> submeshes) {
        _submeshes = submeshes.ToArray();
    }

    public bool TryGetVertexBuffer(uint channel, out VertexBuffer buffer, out uint stride) {
        int index = 0;

        foreach (var descriptor in _vertexDescs) {
            if (descriptor.Channel == channel) {
                buffer = _vertexBuffers[index];
                stride = descriptor.Stride;
                return true;
            }

            index++;
        }

        buffer = default;
        stride = 0;
        return false;
    }

    public VertexBuffer GetVertexBuffer(uint channel) => TryGetVertexBuffer(channel, out var buffer, out _) ? buffer : default;
    public VertexBuffer GetVertexBuffer(uint channel, out uint stride) => TryGetVertexBuffer(channel, out var buffer, out stride) ? buffer : default;

    protected override void Dispose() {
        IndexBuffer?.DecrementReference();
        IndexBuffer = null;
        
        _vertexCount = _indexCount = 0;
        _vertexDescs = Array.Empty<VertexDescriptor>();
        _submeshes = Array.Empty<SubmeshInformation>();

        foreach ((var buffer, var view) in _vertexBuffers) {
            buffer.DecrementReference();
            view.DecrementReference();
        }
        _vertexBuffers = Array.Empty<VertexBuffer>();
    }

    private static void ValidateVertexDescriptors(ReadOnlySpan<VertexDescriptor> descriptors) {
        ushort duplicateChannelFlags = 0;

        int i = 0;
        foreach (ref readonly var descriptor in descriptors) {
            if (descriptor.Channel >= 16) throw new ArgumentException($"Vertex descriptor index {i} is using channel {descriptor.Channel}. Only channel 0 to 15 (inclusive) is allowed.");
            if ((duplicateChannelFlags & 1U << (int)descriptor.Channel) != 0) throw new ArgumentException($"Multiple vertex descriptors use same channel {descriptor.Channel} detected.", nameof(descriptors));
            if (descriptor.Stride == 0) throw new ArgumentException($"Vertex descriptor index {i} has stride value of 0, which is not allowed.");
            if (descriptor.Stride % 4 != 0) throw new ArgumentException($"Vertex descriptor index {i} has stride value of {descriptor.Stride}. Only multiplier of 4 is allowed.");
            
            i++;
            duplicateChannelFlags |= (byte)(1 << (int)descriptor.Channel);
        }
    }

    public readonly record struct VertexBuffer(GpuBuffer Buffer, ShaderResourceView View);
}