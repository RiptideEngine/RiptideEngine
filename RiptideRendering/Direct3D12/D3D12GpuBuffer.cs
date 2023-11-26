namespace RiptideRendering.Direct3D12; 

internal sealed unsafe class D3D12GpuBuffer : GpuBuffer {
    public const string UnnamedResource = $"<Unnamed {nameof(D3D12GpuBuffer)}>.{nameof(NativeResourceHandle)}";
    
    private D3D12RenderingContext _context;

    public override BufferDescription Description {
        get {
            if (NativeResourceHandle == nint.Zero) return default;

            ResourceDesc rdesc = ((ID3D12Resource*)NativeResourceHandle)->GetDesc();

            return new() {
                Width = rdesc.Width,
                Flags = BufferFlags.None,
            };
        }
    }
    
    public override string? Name {
        get => base.Name;
        set {
            if (Interlocked.Read(ref _refcount) == 0) return;
            
            base.Name = value;
            D3D12Helper.SetName((ID3D12Resource*)NativeResourceHandle, value == null ? UnnamedResource : $"{value}.{nameof(NativeResourceHandle)}");
        }
    }

    internal ResourceStates UsageState { get; set; }
    internal ResourceStates TransitioningState { get; set; }
    
    public D3D12GpuBuffer(D3D12RenderingContext context, BufferDescription desc) {
        HeapProperties hprops = new() {
            Type = HeapType.Default,
            CreationNodeMask = 1,
            VisibleNodeMask = 1,
        };
        ResourceDesc rdesc = new() {
            Dimension = ResourceDimension.Buffer,
            Width = desc.Width,
            Height = 1,
            DepthOrArraySize = 1,
            Alignment = 0,
            Format = Format.FormatUnknown,
            MipLevels = 1,
            SampleDesc = new() {
                Count = 1,
                Quality = 0,
            },
            Layout = TextureLayout.LayoutRowMajor,
            Flags = ResourceFlags.None,
        };
        
        using ComPtr<ID3D12Resource> pOutput = default;
        int hr = context.Device->CreateCommittedResource(&hprops, HeapFlags.None, &rdesc, D3D12ResourceStates.Common, null, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)pOutput.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);
        
        D3D12Helper.SetName(pOutput.Handle, UnnamedResource);

        NativeResourceHandle = (nint)pOutput.Detach();
        _context = context;

        UsageState = ResourceStates.Common;
        TransitioningState = (ResourceStates)(-1);
        
        _refcount = 1;
    }

    protected override void Dispose() {
        _context.AddToDeferredDestruction((ID3D12Resource*)NativeResourceHandle);
        NativeResourceHandle = nint.Zero;

        _context = null!;
    }
}