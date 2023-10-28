namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12ReadbackBuffer : ReadbackBuffer {
    public const string UnnamedResource = $"<Unnamed {nameof(D3D12ReadbackBuffer)}>.{nameof(ResourceHandle)}";

    public override string? Name {
        get => base.Name;
        set {
            base.Name = value;

            if (ResourceHandle.Handle != 0) {
                if (value == null) {
                    D3D12Helper.SetName((ID3D12Resource*)ResourceHandle.Handle, UnnamedResource);
                } else {
                    var reqLength = value.Length + 1 + nameof(ResourceHandle).Length;
                    var charArray = ArrayPool<char>.Shared.Rent(reqLength);
                    {
                        value.CopyTo(charArray);
                        charArray[value.Length] = '.';
                        nameof(ResourceHandle).CopyTo(0, charArray, value.Length + 1, nameof(ResourceHandle).Length);

                        D3D12Helper.SetName((ID3D12Resource*)ResourceHandle.Handle, charArray.AsSpan(0, reqLength));
                    }
                    ArrayPool<char>.Shared.Return(charArray);
                }
            }
        }
    }

    private void* pMappedPointer;

    private ReadbackBufferDescriptor _descriptor;
    public override ReadbackBufferDescriptor Descriptor => _descriptor;

    private D3D12RenderingContext _context;

    public D3D12ReadbackBuffer(D3D12RenderingContext context, in ReadbackBufferDescriptor descriptor) {
        HeapProperties hprops = new() {
            Type = HeapType.Readback,
            CPUPageProperty = CpuPageProperty.Unknown,
            MemoryPoolPreference = MemoryPool.Unknown,
            CreationNodeMask = 1,
            VisibleNodeMask = 1,
        };
        ResourceDesc rdesc = new() {
            Dimension = ResourceDimension.Buffer,
            Alignment = 0,
            Width = descriptor.Size,
            Height = 1,
            DepthOrArraySize = 1,
            Flags = ResourceFlags.None,
            MipLevels = 1,
            Format = Format.FormatUnknown,
            Layout = TextureLayout.LayoutRowMajor,
            SampleDesc = new() {
                Count = 1,
                Quality = 0,
            },
        };

        using ComPtr<ID3D12Resource> outputResource = default;
        int hr = context.Device->CreateCommittedResource(&hprops, HeapFlags.None, &rdesc, D3D12ResourceStates.CopyDest, null, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)outputResource.GetAddressOf());

        D3D12Helper.SetName(outputResource.Handle, UnnamedResource);

        hr = outputResource.Map(0, (D3D12Range*)null, ref pMappedPointer);

        ResourceHandle = new((ulong)outputResource.Detach());

        _context = context;
        _refcount = 1;

        _descriptor = descriptor;
    }

    protected override void Dispose() {
        pMappedPointer = null;

        _context.AddToDeferredDestruction((ID3D12Resource*)ResourceHandle.Handle); ResourceHandle = default;
        _context = null!;

        _descriptor = default;
    }

    public override ReadOnlySpan<byte> GetMappedData() {
        return new(pMappedPointer, (int)_descriptor.Size);
    }
}