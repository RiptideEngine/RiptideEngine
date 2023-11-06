namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12RenderTargetView : RenderTargetView {
    public D3D12RenderTargetView(D3D12RenderingContext context, D3D12GpuResource resource, RenderTargetViewDescriptor descriptor) {
        var handle = context.GetResourceDescriptorAllocator(DescriptorHeapType.Rtv).Allocate(1);
        var convert = D3D12Convert.TryConvert(descriptor.Format, out var format);
        Debug.Assert(convert);

        RenderTargetViewDesc desc = new() {
            ViewDimension = (RtvDimension)(descriptor.Dimension + 1),
            Format = format,
        };

        switch (descriptor.Dimension) {
            case RenderTargetViewDimension.Buffer:
                ref readonly var buffer = ref descriptor.Buffer;

                desc.Buffer = new() {
                    FirstElement = buffer.FirstElement,
                    NumElements = buffer.NumElements,
                };
                break;

            case RenderTargetViewDimension.Texture1D:
                ref readonly var tex1d = ref descriptor.Texture1D;

                desc.Texture1D = new() {
                    MipSlice = tex1d.MipSlice,
                };
                break;

            case RenderTargetViewDimension.Texture1DArray:
                ref readonly var tex1darr = ref descriptor.Texture1DArray;

                desc.Texture1DArray = new() {
                    MipSlice = tex1darr.MipSlice,
                    FirstArraySlice = tex1darr.FirstArraySlice,
                    ArraySize = tex1darr.ArraySize,
                };
                break;

            case RenderTargetViewDimension.Texture2D:
                ref readonly var tex2d = ref descriptor.Texture2D;

                desc.Texture2D = new() {
                    MipSlice = tex2d.MipSlice,
                    PlaneSlice = tex2d.PlaneSlice,
                };
                break;

            case RenderTargetViewDimension.Texture2DArray:
                ref readonly var tex2darr = ref descriptor.Texture2DArray;

                desc.Texture2DArray = new() {
                    MipSlice = tex2darr.MipSlice,
                    PlaneSlice = tex2darr.PlaneSlice,
                    FirstArraySlice = tex2darr.FirstArraySlice,
                    ArraySize = tex2darr.ArraySize,
                };
                break;

            case RenderTargetViewDimension.Texture2DMS: break;

            case RenderTargetViewDimension.Texture2DMSArray:
                ref readonly var tex2dmsarr = ref descriptor.Texture2DMSArray;

                desc.Texture2DMSArray = new() {
                    FirstArraySlice = tex2dmsarr.FirstArraySlice,
                    ArraySize = tex2dmsarr.ArraySize,
                };
                break;

            case RenderTargetViewDimension.Texture3D:
                ref readonly var tex3d = ref descriptor.Texture3D;

                desc.Texture3D = new() {
                    MipSlice = tex3d.MipSlice,
                    FirstWSlice = tex3d.FirstWSlice,
                    WSize = tex3d.WSize,
                };
                break;
        };

        context.Device->CreateRenderTargetView((ID3D12Resource*)resource.NativeResource.Handle, &desc, handle);
        NativeView = new(handle.Ptr);
        _refcount = 1;
    }

    protected override void Dispose() {
        NativeView = default;
    }
}