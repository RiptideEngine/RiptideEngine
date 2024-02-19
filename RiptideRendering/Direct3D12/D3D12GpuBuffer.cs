﻿using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12GpuBuffer : GpuBuffer, IResourceStateTracking {
    public const string UnnamedResource = $"<Unnamed {nameof(D3D12GpuBuffer)}>.{nameof(NativeResourceHandle)}";
    
    private D3D12RenderingContext _context;

    public override BufferDescription Description {
        get {
            if (NativeResourceHandle == nint.Zero) return default;

            ResourceDesc rdesc = ((ID3D12Resource*)NativeResourceHandle)->GetDesc();

            return new() {
                Width = rdesc.Width,
                Flags = rdesc.Flags.HasFlag(ResourceFlags.AllowUnorderedAccess) ? BufferFlags.UnorderedAccess : 0,
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
    
    public D3D12GpuBuffer(D3D12RenderingContext context, BufferDescription desc) {
        HeapProperties hprops = new() {
            Type = desc.Type switch {
                BufferType.Default => HeapType.Default,
                BufferType.Dynamic => HeapType.Upload,
                _ => throw new NotImplementedException($"Unimplemented buffer type '{desc.Type}'"),
            },
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
            Flags = desc.Flags.HasFlag(BufferFlags.UnorderedAccess) ? ResourceFlags.AllowUnorderedAccess : 0,
        };

        UsageState = desc.Type == BufferType.Dynamic ? ResourceStates.GenericRead : ResourceStates.Common;
        
        using ComPtr<ID3D12Resource> pOutput = default;
        int hr = context.Device->CreateCommittedResource(&hprops, HeapFlags.None, &rdesc, UsageState, null, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)pOutput.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);
        
        Helper.SetName(pOutput.Handle, UnnamedResource);

        NativeResourceHandle = (nint)pOutput.Detach();
        _context = context;

        TransitioningState = (ResourceStates)(-1);
        
        _refcount = 1;
    }

    protected override void Dispose() {
        _context.DestroyDeferred((ID3D12Resource*)NativeResourceHandle);
        NativeResourceHandle = nint.Zero;

        _context = null!;
    }
}