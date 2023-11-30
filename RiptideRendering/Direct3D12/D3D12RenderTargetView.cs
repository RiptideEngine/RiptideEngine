using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12RenderTargetView : RenderTargetView {
    public CpuDescriptorHandle Handle { get; private set; }
    
    public D3D12RenderTargetView(D3D12RenderingContext context, D3D12GpuTexture texture, RenderTargetViewDescription desc) {
        Unsafe.SkipInit(out RenderTargetViewDesc rtvdesc);
        
        var convert = Converting.TryConvert(desc.Format, out rtvdesc.Format);
        Debug.Assert(convert);

        switch (desc.Dimension) {
            case RenderTargetViewDimension.Texture1D:
                ref readonly var tex1d = ref desc.Texture1D;

                rtvdesc.ViewDimension = RtvDimension.Texture1D;
                rtvdesc.Texture1D = new() {
                    MipSlice = tex1d.MipSlice,
                };
                break;

            case RenderTargetViewDimension.Texture1DArray:
                ref readonly var tex1darr = ref desc.Texture1DArray;

                rtvdesc.ViewDimension = RtvDimension.Texture1Darray;
                rtvdesc.Texture1DArray = new() {
                    MipSlice = tex1darr.MipSlice,
                    FirstArraySlice = tex1darr.FirstArraySlice,
                    ArraySize = tex1darr.ArraySize,
                };
                break;

            case RenderTargetViewDimension.Texture2D:
                ref readonly var tex2d = ref desc.Texture2D;

                rtvdesc.ViewDimension = RtvDimension.Texture2D;
                rtvdesc.Texture2D = new() {
                    MipSlice = tex2d.MipSlice,
                    PlaneSlice = tex2d.PlaneSlice,
                };
                break;

            case RenderTargetViewDimension.Texture2DArray:
                ref readonly var tex2darr = ref desc.Texture2DArray;

                rtvdesc.ViewDimension = RtvDimension.Texture2Darray;
                rtvdesc.Texture2DArray = new() {
                    MipSlice = tex2darr.MipSlice,
                    PlaneSlice = tex2darr.PlaneSlice,
                    FirstArraySlice = tex2darr.FirstArraySlice,
                    ArraySize = tex2darr.ArraySize,
                };
                break;

            // case RenderTargetViewDimension.Texture2DMS: break;
            //
            // case RenderTargetViewDimension.Texture2DMSArray:
            //     ref readonly var tex2dmsarr = ref desc.Texture2DMSArray;
            //
            //     rtvdesc.Texture2DMSArray = new() {
            //         FirstArraySlice = tex2dmsarr.FirstArraySlice,
            //         ArraySize = tex2dmsarr.ArraySize,
            //     };
            //     break;

            case RenderTargetViewDimension.Texture3D:
                ref readonly var tex3d = ref desc.Texture3D;

                rtvdesc.Texture3D = new() {
                    MipSlice = tex3d.MipSlice,
                    FirstWSlice = tex3d.FirstWSlice,
                    WSize = tex3d.WSize,
                };
                break;
        }

        Handle = context.AllocateCpuDescriptor(DescriptorHeapType.Rtv);
        context.Device->CreateRenderTargetView((ID3D12Resource*)texture.NativeResourceHandle, &rtvdesc, Handle);
        _refcount = 1;
    }

    public D3D12RenderTargetView(CpuDescriptorHandle handle) {
        Handle = handle;
        _refcount = 1;
    }

    protected override void Dispose() {
        Handle = default;
    }
}