namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12GpuResource : GpuResource {
    public const string UnnamedResource = $"<Unnamed {nameof(D3D12GpuResource)}>.{nameof(NativeResource)}";

    public override string? Name {
        get => base.Name;
        set {
            base.Name = value;

            if (NativeResource.Handle != 0) {
                if (value == null) {
                    D3D12Helper.SetName((ID3D12Resource*)NativeResource.Handle, UnnamedResource);
                } else {
                    var reqLength = value.Length + 1 + nameof(NativeResource).Length;
                    var charArray = ArrayPool<char>.Shared.Rent(reqLength);
                    {
                        value.CopyTo(charArray);
                        charArray[value.Length] = '.';
                        nameof(NativeResource).CopyTo(0, charArray, value.Length + 1, nameof(NativeResource).Length);

                        D3D12Helper.SetName((ID3D12Resource*)NativeResource.Handle, charArray.AsSpan(0, reqLength));
                    }
                    ArrayPool<char>.Shared.Return(charArray);
                }
            }
        }
    }

    private D3D12RenderingContext _context;

    public override ResourceDescriptor Descriptor {
        get {
            if (NativeResource.Handle == 0) return default;

            ResourceDesc rdesc = ((ID3D12Resource*)NativeResource.Handle)->GetDesc();

            return new() {
                Dimension = (ResourceDimension)rdesc.Dimension,
                Width = rdesc.Width,
                Height = (ushort)rdesc.Height,
                DepthOrArraySize = rdesc.DepthOrArraySize,
                
                Flags = (rdesc.Flags.HasFlag(D3D12ResourceFlags.AllowDepthStencil) ? ResourceFlags.DepthStencil : 0) |
                        (rdesc.Flags.HasFlag(D3D12ResourceFlags.AllowRenderTarget) ? ResourceFlags.RenderTarget : 0) |
                        (rdesc.Flags.HasFlag(D3D12ResourceFlags.AllowUnorderedAccess) ? ResourceFlags.UnorderedAccess : 0),
            };
        }
    }

    public D3D12GpuResource(D3D12RenderingContext context, in ResourceDescriptor desc, ResourceStates initialStates) {
        HeapProperties hprops = new() {
            CPUPageProperty = CpuPageProperty.Unknown,
            MemoryPoolPreference = MemoryPool.Unknown,
            CreationNodeMask = 1,
            VisibleNodeMask = 1,
            Type = HeapType.Default,
        };

        bool convert = D3D12Convert.TryConvert(desc.TextureFormat, out var format);
        Debug.Assert(convert);

        ResourceDesc rdesc = new() {
            Dimension = (D3D12ResourceDimension)desc.Dimension,
            Width = desc.Width,
            Height = desc.Height,
            DepthOrArraySize = desc.DepthOrArraySize,
            Alignment = 0,
            SampleDesc = new() {
                Count = 1,
                Quality = 0,
            },
            MipLevels = 1,
            Layout = desc.Dimension == ResourceDimension.Buffer ? TextureLayout.LayoutRowMajor : TextureLayout.LayoutUnknown,
            Format = format,
            Flags = (desc.Flags.HasFlag(ResourceFlags.RenderTarget) ? D3D12ResourceFlags.AllowRenderTarget : 0) |
                    (desc.Flags.HasFlag(ResourceFlags.DepthStencil) ? D3D12ResourceFlags.AllowDepthStencil : 0) |
                    (desc.Flags.HasFlag(ResourceFlags.UnorderedAccess) ? D3D12ResourceFlags.AllowUnorderedAccess : 0),
        };

        scoped Span<ClearValue> clear = null;
        if (desc.Flags.HasFlag(ResourceFlags.DepthStencil)) {
            clear = [
                new() {
                    Format = format,
                    DepthStencil = new() {
                        Depth = 1,
                        Stencil = 0,
                    }
                }
            ];
        } else if (desc.Flags.HasFlag(ResourceFlags.RenderTarget)) {
            clear = [
                new() {
                    Format = format,
                }
            ];
            clear[0].Anonymous.Color[0] = 0;
            clear[0].Anonymous.Color[1] = 0;
            clear[0].Anonymous.Color[2] = 0;
            clear[0].Anonymous.Color[3] = 1;
        }

        var device = context.Device;
        int hr;

        using ComPtr<ID3D12Resource> pResource = default;
        fixed (ClearValue* pClear = clear) {
            hr = device->CreateCommittedResource(&hprops, HeapFlags.None, &rdesc, D3D12Convert.Convert(initialStates), pClear, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)pResource.GetAddressOf());
        }
        Marshal.ThrowExceptionForHR(hr);

        D3D12Helper.SetName(pResource.Handle, UnnamedResource);

        NativeResource = new((nint)pResource.Detach());

        _context = context;
    }

    public D3D12GpuResource(D3D12RenderingContext context, IDXGISwapChain* pSwapchain, uint bufferIndex) {
        ID3D12Resource* pOutput;
        int hr = pSwapchain->GetBuffer(bufferIndex, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)&pOutput);
        Marshal.ThrowExceptionForHR(hr);

        NativeResource = new((nint)pOutput);

        _context = context;
    }

    public D3D12GpuResource(D3D12RenderingContext context, HeapProperties* pHeapProperties, ResourceDesc* pResourceDesc, D3D12ResourceStates initialStates = D3D12ResourceStates.Common) {
        var device = context.Device;

        using ComPtr<ID3D12Resource> pResource = default;
        int hr = device->CreateCommittedResource(pHeapProperties, HeapFlags.None, pResourceDesc, initialStates, null, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)pResource.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);

        D3D12Helper.SetName(pResource.Handle, UnnamedResource);

        NativeResource = new((nint)pResource.Detach());

        _context = context;
    }

    protected override void Dispose() {
        _context.AddToDeferredDestruction((ID3D12Resource*)NativeResource.Handle);
        NativeResource = default;

        _context = null!;
    }
}