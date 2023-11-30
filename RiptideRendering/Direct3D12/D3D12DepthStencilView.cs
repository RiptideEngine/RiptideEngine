using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12DepthStencilView : DepthStencilView {
    public CpuDescriptorHandle Handle { get; private set; }
    
    [SkipLocalsInit]
    public D3D12DepthStencilView(D3D12RenderingContext context, D3D12GpuTexture texture, DepthStencilViewDescription desc) {
        Unsafe.SkipInit(out DepthStencilViewDesc dsvdesc);
        
        bool convert = Converting.TryConvert(desc.Format, out dsvdesc.Format);
        Debug.Assert(convert);

        dsvdesc.Flags = DsvFlags.None;

        switch (desc.Dimension) {
            case DepthStencilViewDimension.Texture1D:
                ref readonly var tex1d = ref desc.Texture1D;

                dsvdesc.ViewDimension = DsvDimension.Texture1D;
                dsvdesc.Texture1D = new() {
                    MipSlice = tex1d.MipSlice,
                };
                break;

            case DepthStencilViewDimension.Texture1DArray:
                ref readonly var tex1darr = ref desc.Texture1DArray;

                dsvdesc.ViewDimension = DsvDimension.Texture1Darray;
                dsvdesc.Texture1DArray = new() {
                    MipSlice = tex1darr.MipSlice,
                    FirstArraySlice = tex1darr.FirstArraySlice,
                    ArraySize = tex1darr.ArraySize,
                };
                break;

            case DepthStencilViewDimension.Texture2D:
                ref readonly var tex2d = ref desc.Texture2D;

                dsvdesc.ViewDimension = DsvDimension.Texture2D;
                dsvdesc.Texture2D = new() {
                    MipSlice = tex2d.MipSlice,
                };
                break;

            case DepthStencilViewDimension.Texture2DArray:
                ref readonly var tex2darr = ref desc.Texture2DArray;

                dsvdesc.ViewDimension = DsvDimension.Texture2Darray;
                dsvdesc.Texture2DArray = new() {
                    MipSlice = tex2darr.MipSlice,
                    FirstArraySlice = tex2darr.FirstArraySlice,
                    ArraySize = tex2darr.ArraySize,
                };
                break;

            // case DepthStencilViewDimension.Texture2DMS: break;
            //
            // case DepthStencilViewDimension.Texture2DMSArray:
            //     ref readonly var tex2dmsarr = ref descriptor.Texture2DMSArray;
            //
            //     desc.Texture2DMSArray = new() {
            //         FirstArraySlice = tex2dmsarr.FirstArraySlice,
            //         ArraySize = tex2dmsarr.ArraySize,
            //     };
            //     break;
            
            default: throw new UnreachableException();
        }
        
        Handle = context.AllocateCpuDescriptor(DescriptorHeapType.Dsv);
        context.Device->CreateDepthStencilView((ID3D12Resource*)texture.NativeResourceHandle, &dsvdesc, Handle);
        
        _refcount = 1;
    }

    protected override void Dispose() {
        Handle = default;
    }
}