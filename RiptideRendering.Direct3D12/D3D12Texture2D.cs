namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12Texture2D : Texture2D {
    private const string UnnamedResource = $"<Unnamed {nameof(D3D12Texture2D)}>.{nameof(ResourceHandle)}";

    private D3D12RenderingContext _context;

    public override Texture2DDescriptor Descriptor {
        get {
            if (ResourceHandle.Handle == 0) return default;

            var rdesc = ((ID3D12Resource*)ResourceHandle.Handle)->GetDesc();

            Texture2DDescriptor descriptor = new() {
                Width = (uint)rdesc.Width,
                Height = rdesc.Height,
            };
            bool cvt = D3D12Convert.TryConvert(rdesc.Format, out descriptor.Format);
            Debug.Assert(cvt);

            return descriptor;
        }
    }

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

    public D3D12Texture2D(D3D12RenderingContext context, in Texture2DDescriptor descriptor) {
        HeapProperties hprops = new() {
            Type = HeapType.Default,
            CreationNodeMask = 1,
            VisibleNodeMask = 1,
            CPUPageProperty = CpuPageProperty.Unknown,
            MemoryPoolPreference = MemoryPool.Unknown,
        };
        ResourceDesc rdesc = new() {
            Dimension = ResourceDimension.Texture2D,
            Alignment = 0,
            Width = descriptor.Width,
            Height = descriptor.Height,
            DepthOrArraySize = 1,
            Layout = TextureLayout.LayoutUnknown,
            MipLevels = 1,
            SampleDesc = new() {
                Count = 1,
                Quality = 0,
            },
            Flags = ResourceFlags.None,
        };
        bool op = D3D12Convert.TryConvert(descriptor.Format, out rdesc.Format);
        Debug.Assert(op);

        var device = context.Device;

        using ComPtr<ID3D12Resource> pOutput = default;
        int hr = device->CreateCommittedResource(&hprops, HeapFlags.None, &rdesc, D3D12ResourceStates.Common, null, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)pOutput.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);

        ResourceHandle = new((ulong)pOutput.Detach());
        D3D12Helper.SetName((ID3D12Resource*)ResourceHandle.Handle, UnnamedResource);

        var handle = context.GetResourceDescriptorAllocator(DescriptorHeapType.CbvSrvUav).Allocate(1);
        device->CreateShaderResourceView((ID3D12Resource*)ResourceHandle.Handle, null, handle);
        ViewHandle = new(handle.Ptr);

        _refcount = 1;
        _context = context;
    }

    protected override void Dispose() {
        _context.AddToDeferredDestruction((ID3D12Resource*)ResourceHandle.Handle); ResourceHandle = default;
        ViewHandle = default;
        _context = null!;
    }
}