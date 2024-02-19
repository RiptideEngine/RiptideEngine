using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace RiptideRendering.Direct3D12;

// TODO: Small Texture optimization.

internal sealed unsafe class D3D12GpuTexture : GpuTexture, IResourceStateTracking {
    public const string UnnamedResource = $"<Unnamed {nameof(D3D12GpuTexture)}>.{nameof(NativeResourceHandle)}";

    private D3D12RenderingContext _context;
    
    public override TextureDescription Description {
        get {
            if (NativeResourceHandle == nint.Zero) return default;

            ResourceDesc rdesc = ((ID3D12Resource*)NativeResourceHandle)->GetDesc();

            bool convert = Converting.TryConvert(rdesc.Format, out var format);
            Debug.Assert(convert);
            
            return new() {
                Dimension = (TextureDimension)(rdesc.Dimension - 1),
                Width = (uint)rdesc.Width,
                Height = (ushort)rdesc.Height,
                DepthOrArraySize = rdesc.DepthOrArraySize,
                Format = format,
                Flags = (rdesc.Flags.HasFlag(ResourceFlags.AllowDepthStencil) ? TextureFlags.DepthStencil : 0) | (rdesc.Flags.HasFlag(ResourceFlags.AllowRenderTarget) ? TextureFlags.RenderTarget : 0),
            };
        }
    }

    public override string? Name {
        get => base.Name;
        set {
            if (Interlocked.Read(ref _refcount) == 0) return;
            
            base.Name = value;
            Helper.SetName((ID3D12Resource*)NativeResourceHandle, value == null ? UnnamedResource : $"{value}.{nameof(NativeResourceHandle)}");
        }
    }

    public ID3D12Resource* TransitionResource => (ID3D12Resource*)NativeResourceHandle;
    public ResourceStates UsageState { get; set; }
    public ResourceStates TransitioningState { get; set; }

    public D3D12GpuTexture(D3D12RenderingContext context, in TextureDescription desc) {
        bool convert = Converting.TryConvert(desc.Format, out var dxgiFormat);
        Debug.Assert(convert);

        HeapProperties hprops = new() {
            Type = HeapType.Default,
            CreationNodeMask = 1,
            VisibleNodeMask = 1,
        };
        ResourceDesc rdesc = new() {
            Dimension = ResourceDimension.Texture1D + (int)desc.Dimension - 1,
            Width = desc.Width,
            Height = desc.Dimension == TextureDimension.Texture1D ? 1u : desc.Height,
            DepthOrArraySize = desc.DepthOrArraySize,
            Alignment = 0,
            Format = dxgiFormat,
            MipLevels = (ushort)desc.MipLevels,
            SampleDesc = new() {
                Count = 1,
                Quality = 0,
            },
            Flags = (desc.Flags.HasFlag(TextureFlags.RenderTarget) ? ResourceFlags.AllowRenderTarget : 0) | (desc.Flags.HasFlag(TextureFlags.DepthStencil) ? ResourceFlags.AllowDepthStencil : 0) | (desc.Flags.HasFlag(TextureFlags.UnorderedAccess) ? ResourceFlags.AllowUnorderedAccess : 0),
        };

        using ComPtr<ID3D12Resource> pOutput = default;
        int hr;
        
        if (rdesc.Flags.HasFlag(ResourceFlags.AllowDepthStencil)) {
            convert = Converting.TryConvertToDepthClearFormat(desc.Format, out var clearFormat);
            Debug.Assert(convert, $"Failed to convert '{desc.Format}' to correspond DXGI_FORMAT.");
            
            ClearValue clear = new() {
                Format = clearFormat,
                DepthStencil = new() {
                    Depth = 1,
                    Stencil = 0,
                },
            };
            
            hr = context.Device->CreateCommittedResource(&hprops, HeapFlags.None, &rdesc, ResourceStates.Common, clear, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)pOutput.GetAddressOf());
        } else if (rdesc.Flags.HasFlag(ResourceFlags.AllowRenderTarget)) {
            ClearValue clear = new() {
                Format = dxgiFormat,
            };
            Unsafe.Write(clear.Anonymous.Color, Vector4.UnitW);
            
            hr = context.Device->CreateCommittedResource(&hprops, HeapFlags.None, &rdesc, ResourceStates.Common, &clear, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)pOutput.GetAddressOf());
        } else {
            hr = context.Device->CreateCommittedResource(&hprops, HeapFlags.None, &rdesc, ResourceStates.Common, null, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)pOutput.GetAddressOf());
        }

        Marshal.ThrowExceptionForHR(hr);
        
        Helper.SetName(pOutput.Handle, UnnamedResource);

        NativeResourceHandle = (nint)pOutput.Detach();

        UsageState = ResourceStates.Common;
        TransitioningState = (ResourceStates)(-1);

        _context = context;
        _refcount = 1;
    }
    
    public D3D12GpuTexture(D3D12RenderingContext context, IDXGISwapChain* pSwapchain, uint bufferIndex) {
        ID3D12Resource* output;
        int hr = pSwapchain->GetBuffer(bufferIndex, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)&output);
        Marshal.ThrowExceptionForHR(hr);

        NativeResourceHandle = (nint)output;
        _context = context;

        UsageState = ResourceStates.Present;
        TransitioningState = (ResourceStates)(-1);

        _refcount = 1;
    }
    
    protected override void Dispose() {
        _context.DestroyDeferred((ID3D12Resource*)NativeResourceHandle);
        NativeResourceHandle = nint.Zero;
        _context = null!;
    }
}