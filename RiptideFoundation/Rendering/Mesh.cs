namespace RiptideFoundation.Rendering;

public readonly record struct VertexDescriptor(uint Stride, uint Channel);

public readonly struct SubmeshInformation(Bound3D aabb, uint indexByteOffset, uint indexByteSize, IndexFormat indexFormat) {
    public readonly Bound3D AABB = aabb;
    public readonly uint IndexByteOffset = indexByteOffset;
    public readonly uint IndexByteSize = indexByteSize;
    public readonly IndexFormat IndexFormat = indexFormat.IsDefined() ? indexFormat : IndexFormat.UInt16;

    public uint StartIndex => IndexByteOffset >> 1 >> (int)IndexFormat;
    public uint IndexCount => IndexByteSize >> 1 >> (int)IndexFormat;
}

public sealed class Mesh : RenderingObject {
    public const int MaximumVertexChannels = 16;
    
    private VertexDescriptor[] _vertexDescs;
    private Buffer[] _vertexBuffers;
    private SubmeshInformation[] _submeshes;

    private uint _vertexCount;
    
    public GpuBuffer? IndexBuffer { get; private set; }

    public uint VertexCount => _vertexCount;
    public uint IndexSize => IndexBuffer == null ? 0 : (uint)IndexBuffer.Description.Width;
    
    public ReadOnlySpan<VertexDescriptor> VertexDescriptors => _vertexDescs;
    public ReadOnlySpan<SubmeshInformation> Submeshes => _submeshes;

    public override string? Name {
        get => base.Name;
        set {
            if (GetReferenceCount() == 0) return;
            
            base.Name = value;

            if (IndexBuffer != null) IndexBuffer.Name = $"{value}.IndexBuffer";
            for (int i = 0; i < _vertexDescs.Length; i++) {
                _vertexBuffers[i].Name = $"{value}.VertexBuffer_C{_vertexDescs[i].Channel}";
            }            
        }
    }

    public Mesh() {
        _vertexDescs = [];
        _vertexBuffers = [];
        _submeshes = [];

        _refcount = 1;
    }

    public void AllocateVertexBuffers(uint numVertices) {
        if (_vertexDescs.Length == 0) throw new InvalidOperationException("Vertex buffer resizing is not allowed before allocation."); 
        
        if (numVertices == 0) {
            foreach (var buffer in _vertexBuffers) {
                buffer.DecrementReference();
            }
            
            _vertexBuffers = [];
            _vertexCount = 0;
            
            return;
        }

        if (_vertexDescs.Length == 0) {
            _vertexCount = 0;
            return;
        }
        
        Buffer[] outputs = new Buffer[_vertexDescs.Length];
        
        try {
            ShaderResourceViewDescription srvdesc = new() {
                Dimension = ShaderResourceViewDimension.Buffer,
                Buffer = new() {
                    FirstElement = 0,
                    NumElements = numVertices,
                },
            };

            for (int i = 0; i < _vertexDescs.Length; i++) {
                ref readonly var descriptor = ref _vertexDescs[i];

                srvdesc.Buffer.StructureSize = descriptor.Stride;
                outputs[i] = new(new() {
                    Flags = BufferFlags.None,
                    Type = BufferType.Default,
                    Width = numVertices * descriptor.Stride,
                }, srvdesc, default);
            }
        } catch {
            foreach (var output in outputs) {
                output?.DecrementReference();
            }
            
            throw;
        }
        
        ReleaseAllCurrentVertexBuffers();
        _vertexBuffers = outputs;

        _vertexCount = numVertices;
    }
    public void AllocateVertexBuffers(uint numVertices, ReadOnlySpan<VertexDescriptor> descriptors) {
        if (numVertices == 0 || descriptors.IsEmpty) {
            _vertexCount = numVertices;
            _vertexDescs = descriptors.ToArray();

            ReleaseAllCurrentVertexBuffers();
            _vertexBuffers = [];
            
            return;
        }
        
        ValidateVertexDescriptors(descriptors);

        Buffer[] outputs = new Buffer[descriptors.Length];
        
        try {
            ShaderResourceViewDescription srvdesc = new() {
                Dimension = ShaderResourceViewDimension.Buffer,
                Buffer = new() {
                    FirstElement = 0,
                    NumElements = numVertices,
                },
            };

            for (int i = 0; i < descriptors.Length; i++) {
                ref readonly var descriptor = ref descriptors[i];

                srvdesc.Buffer.StructureSize = descriptor.Stride;
                outputs[i] = new(new() {
                    Flags = BufferFlags.None,
                    Type = BufferType.Default,
                    Width = numVertices * descriptor.Stride,
                }, srvdesc, default);
            }
        } catch {
            foreach (var output in outputs) {
                output?.DecrementReference();
            }
            
            throw;
        }

        _vertexCount = numVertices;

        ReleaseAllCurrentVertexBuffers();
        _vertexBuffers = outputs;

        _vertexDescs = descriptors.ToArray();
    }

    public void AllocateIndexBuffer(uint byteSize) {
        if (byteSize < 2) {
            IndexBuffer?.DecrementReference();
            IndexBuffer = null;
            return;
        }

        if (IndexBuffer != null && IndexBuffer.Description.Width != byteSize) {
            IndexBuffer?.DecrementReference();
        }
        
        IndexBuffer = Graphics.RenderingContext.Factory.CreateBuffer(new() {
            Width = byteSize,
        });
        IndexBuffer.Name = $"{Name}.IndexBuffer";
    }

    public void SetSubmesh(int index, SubmeshInformation submesh) {
        _submeshes[index] = submesh;
    }
    
    public void SetSubmeshes(ReadOnlySpan<SubmeshInformation> submeshes) {
        _submeshes = submeshes.ToArray();
    }

    public bool TryGetVertexBuffer(uint channel, [NotNullWhen(true)] out Buffer? buffer, out uint stride) {
        int index = 0;

        if (_vertexBuffers.Length == 0) {
            buffer = default;
            stride = 0;
            return false;
        }

        foreach (ref readonly var descriptor in _vertexDescs.AsSpan()) {
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

    public Buffer? GetVertexBuffer(uint channel) => TryGetVertexBuffer(channel, out var buffer, out _) ? buffer : null;
    public Buffer? GetVertexBuffer(uint channel, out uint stride) => TryGetVertexBuffer(channel, out var buffer, out stride) ? buffer : null;

    public IEnumerable<(VertexDescriptor Descriptor, Buffer Buffer)> EnumerateVertexBuffers() {
        return _vertexDescs.Zip(_vertexBuffers);
    }

    private void ReleaseAllCurrentVertexBuffers() {
        foreach (var buffer in _vertexBuffers) {
            buffer.DecrementReference();
        }
    }
    
    protected override void Dispose() {
        IndexBuffer?.DecrementReference();
        IndexBuffer = null;
        
        _vertexCount = 0;
        _vertexDescs = [];
        _submeshes = [];

        ReleaseAllCurrentVertexBuffers();
        _vertexBuffers = [];
    }

    internal static void ValidateVertexDescriptors(ReadOnlySpan<VertexDescriptor> descriptors) {
        ushort duplicateChannelFlags = 0;

        int i = 0;
        foreach (ref readonly var descriptor in descriptors) {
            if (descriptor.Channel >= MaximumVertexChannels) throw new ArgumentException($"Vertex descriptor index {i} is using channel {descriptor.Channel}. Only channel 0 to 15 (inclusive) is allowed.");
            if ((duplicateChannelFlags & 1U << (int)descriptor.Channel) != 0) throw new ArgumentException($"Multiple vertex descriptors use same channel {descriptor.Channel} detected.", nameof(descriptors));
            if (descriptor.Stride == 0) throw new ArgumentException($"Vertex descriptor index {i} has stride value of 0, which is not allowed.");
            if (descriptor.Stride % 4 != 0) throw new ArgumentException($"Vertex descriptor index {i} has stride value of {descriptor.Stride}. Only multiplier of 4 is allowed.");
            
            i++;
            duplicateChannelFlags |= (byte)(1 << (int)descriptor.Channel);
        }
    }

    public static bool CheckLayoutCompatibility(ReadOnlySpan<VertexDescriptor> layoutA, ReadOnlySpan<VertexDescriptor> layoutB) {
        foreach (ref readonly var la in layoutB) {
            bool found = false;
            
            foreach (ref readonly var lb in layoutA) {
                if (lb.Channel != la.Channel) continue;
                if (lb.Stride != la.Stride) return false;

                found = true;
                break;
            }

            if (!found) return false;
        }

        return true;
    }
    
    public static bool CheckLayoutCompatibility(IEnumerable<VertexDescriptor> layoutA, ReadOnlySpan<VertexDescriptor> layoutB) {
        foreach (var layout in layoutA) {
            bool found = false;
            
            foreach (ref readonly var compared in layoutB) {
                if (compared.Channel != layout.Channel) continue;
                if (compared.Stride != layout.Stride) return false;

                found = true;
                break;
            }

            if (!found) return false;
        }

        return true;
    }
}