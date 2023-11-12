namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12DepthStencilView : DepthStencilView {
    public D3D12DepthStencilView(D3D12RenderingContext context, D3D12GpuResource resource, DepthStencilViewDescriptor descriptor) {
        var handle = context.GetResourceDescriptorAllocator(DescriptorHeapType.Dsv).Allocate();
        bool convert = D3D12Convert.TryConvert(descriptor.Format, out var format);
        Debug.Assert(convert);

        DepthStencilViewDesc desc = new() {
            Flags = DsvFlags.None,
            Format = format,
            ViewDimension = (DsvDimension)(descriptor.Dimension + 1),
        };

        switch (descriptor.Dimension) {
            case DepthStencilViewDimension.Texture1D:
                ref readonly var tex1d = ref descriptor.Texture1D;

                desc.Texture1D = new() {
                    MipSlice = tex1d.MipSlice,
                };
                break;

            case DepthStencilViewDimension.Texture1DArray:
                ref readonly var tex1darr = ref descriptor.Texture1DArray;

                desc.Texture1DArray = new() {
                    MipSlice = tex1darr.MipSlice,
                    FirstArraySlice = tex1darr.FirstArraySlice,
                    ArraySize = tex1darr.ArraySize,
                };
                break;

            case DepthStencilViewDimension.Texture2D:
                ref readonly var tex2d = ref descriptor.Texture2D;

                desc.Texture2D = new() {
                    MipSlice = tex2d.MipSlice,
                };
                break;

            case DepthStencilViewDimension.Texture2DArray:
                ref readonly var tex2darr = ref descriptor.Texture2DArray;

                desc.Texture1DArray = new() {
                    MipSlice = tex2darr.MipSlice,
                    FirstArraySlice = tex2darr.FirstArraySlice,
                    ArraySize = tex2darr.ArraySize,
                };
                break;

            case DepthStencilViewDimension.Texture2DMS: break;

            case DepthStencilViewDimension.Texture2DMSArray:
                ref readonly var tex2dmsarr = ref descriptor.Texture2DMSArray;

                desc.Texture2DMSArray = new() {
                    FirstArraySlice = tex2dmsarr.FirstArraySlice,
                    ArraySize = tex2dmsarr.ArraySize,
                };
                break;
        }

        context.Device->CreateDepthStencilView((ID3D12Resource*)resource.NativeResource.Handle, &desc, handle);
        NativeView = new(handle.Ptr);
        _refcount = 1;
    }

    protected override void Dispose() {
        NativeView = default;
    }
}