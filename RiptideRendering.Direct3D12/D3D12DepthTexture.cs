namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12DepthTexture : DepthTexture {
    private const string UnnamedResource = $"<Unnamed {nameof(D3D12DepthTexture)}>.{nameof(ResourceHandle)}";

    private D3D12RenderingContext _context;

    public override DepthTextureDescriptor Descriptor {
        get {
            if (ResourceHandle.Handle == 0) return default;

            var rdesc = ((ID3D12Resource*)ResourceHandle.Handle)->GetDesc();

            DepthTextureDescriptor descriptor = new() {
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

    public D3D12DepthTexture(D3D12RenderingContext context, in DepthTextureDescriptor descriptor) {
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
            Flags = ResourceFlags.AllowDepthStencil,
        };
        ClearValue clear = new() {
            Anonymous = new() {
                DepthStencil = new() {
                    Depth = 1,
                    Stencil = 0,
                }
            }
        };
        ShaderResourceViewDesc depthViewDesc = new() {
            Shader4ComponentMapping = D3D12Helper.DefaultShader4ComponentMapping,
            ViewDimension = SrvDimension.Texture2D,
            Texture2D = new() {
                MipLevels = rdesc.MipLevels,
            },
        };
        DepthStencilViewDesc dsvdesc = new() {
            ViewDimension = DsvDimension.Texture2D,
        };

        switch (descriptor.Format) {
            case GraphicsFormat.D16UNorm:
                rdesc.Format = Format.FormatR16Typeless;
                dsvdesc.Format = Format.FormatD16Unorm;
                depthViewDesc.Format = Format.FormatR16Unorm;
                clear.Format = Format.FormatD16Unorm;
                break;

            case GraphicsFormat.D24UNormS8UInt:
                rdesc.Format = Format.FormatR24G8Typeless;
                dsvdesc.Format = Format.FormatD24UnormS8Uint;
                depthViewDesc.Format = Format.FormatR24UnormX8Typeless;
                clear.Format = Format.FormatD24UnormS8Uint;
                break;

            case GraphicsFormat.D32Float:
                rdesc.Format = Format.FormatR32Typeless;
                dsvdesc.Format = Format.FormatD32Float;
                depthViewDesc.Format = Format.FormatR32Float;
                clear.Format = Format.FormatD32Float;
                break;

            case GraphicsFormat.D32FloatS8UInt:
                rdesc.Format = Format.FormatR32G8X24Typeless;
                dsvdesc.Format = Format.FormatD32FloatS8X24Uint;
                depthViewDesc.Format = Format.FormatR32FloatX8X24Typeless;
                clear.Format = Format.FormatD32FloatS8X24Uint;
                break;
        }

        var device = context.Device;

        using ComPtr<ID3D12Resource> pOutput = default;
        int hr = device->CreateCommittedResource(&hprops, HeapFlags.None, &rdesc, D3D12Convert.Convert(descriptor.InitialStates), &clear, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)pOutput.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);

        ResourceHandle = new((ulong)pOutput.Detach());
        D3D12Helper.SetName((ID3D12Resource*)ResourceHandle.Handle, UnnamedResource);

        var handle = context.GetResourceDescriptorAllocator(DescriptorHeapType.CbvSrvUav).Allocate(1);
        device->CreateShaderResourceView((ID3D12Resource*)ResourceHandle.Handle, &depthViewDesc, handle);
        ViewHandle = new(handle.Ptr);

        handle = context.GetResourceDescriptorAllocator(DescriptorHeapType.Dsv).Allocate(1);
        device->CreateDepthStencilView((ID3D12Resource*)ResourceHandle.Handle, &dsvdesc, handle);
        DepthStencilHandle = new(handle.Ptr);

        _refcount = 1;
        _context = context;
    }

    protected override void Dispose() {
        _context.AddToDeferredDestruction((ID3D12Resource*)ResourceHandle.Handle); ResourceHandle = default;
        ViewHandle = default;
        DepthStencilHandle = default;
        _context = null!;
    }
}