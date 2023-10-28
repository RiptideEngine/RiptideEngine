namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12GpuBuffer : GpuBuffer {
    public const string UnnamedResource = $"<Unnamed {nameof(D3D12GpuBuffer)}>.{nameof(ResourceHandle)}";

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

    private D3D12RenderingContext _context;

    public override BufferDescriptor Descriptor {
        get {
            if (ResourceHandle.Handle == 0) return default;

            ResourceDesc rdesc = ((ID3D12Resource*)ResourceHandle.Handle)->GetDesc();

            return new() {
                Size = rdesc.Width,
                Flags = BufferFlags.None,
                //Flags = rdesc.Flags.HasFlag(ResourceFlags.AllowUnorderedAccess) ? BufferFlags.UnorderedAccess : 0,
            };
        }
    }

    public D3D12GpuBuffer(D3D12RenderingContext context, in BufferDescriptor desc) {
        HeapProperties hprops = new() {
            CPUPageProperty = CpuPageProperty.Unknown,
            MemoryPoolPreference = MemoryPool.Unknown,
            CreationNodeMask = 1,
            VisibleNodeMask = 1,
            Type = HeapType.Default,
        };
        ResourceDesc rdesc = new() {
            Dimension = ResourceDimension.Buffer,
            Width = desc.Size,
            Height = 1,
            DepthOrArraySize = 1,
            Alignment = 0,
            SampleDesc = new() {
                Count = 1,
                Quality = 0,
            },
            MipLevels = 1,
            Layout = TextureLayout.LayoutRowMajor,
            Format = Format.FormatUnknown,
            Flags = ResourceFlags.None,
            //Flags = desc.Flags.HasFlag(BufferFlags.UnorderedAccess) ? ResourceFlags.AllowUnorderedAccess : 0,
        };

        var device = context.Device;

        using ComPtr<ID3D12Resource> pResource = default;
        int hr = device->CreateCommittedResource(&hprops, HeapFlags.None, &rdesc, D3D12ResourceStates.Common, null, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)pResource.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);

        ResourceHandle = new((ulong)pResource.Detach());

        D3D12Helper.SetName((ID3D12Resource*)ResourceHandle.Handle, UnnamedResource);

        _context = context;
    }

    protected override void Dispose() {
        _context.AddToDeferredDestruction((ID3D12Resource*)ResourceHandle.Handle); ResourceHandle = default;
        _context = null!;
    }
}