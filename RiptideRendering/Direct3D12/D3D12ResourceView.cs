namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12ResourceView : ResourceView {
    public D3D12ResourceView(D3D12RenderingContext context, D3D12GpuResource resource, ResourceViewDescriptor descriptor) {
        bool convert = D3D12Convert.TryConvert(descriptor.Format, out var format);
        Debug.Assert(convert);

        var handle = context.GetResourceDescriptorAllocator(DescriptorHeapType.CbvSrvUav).Allocate(1);

        ShaderResourceViewDesc desc = new() {
            Shader4ComponentMapping = D3D12Helper.DefaultShader4ComponentMapping,
            ViewDimension = (SrvDimension)(descriptor.Dimension + 1),
            Format = format,
        };

        switch (descriptor.Dimension) {
            case ResourceViewDimension.Buffer:
                ref readonly var buffer = ref descriptor.Buffer;

                desc.Buffer = new() {
                    FirstElement = buffer.FirstElement,
                    NumElements = buffer.NumElements,
                    StructureByteStride = buffer.StructureSize,
                    Flags = buffer.IsRawBuffer ? BufferSrvFlags.Raw : BufferSrvFlags.None,
                };
                break;

            case ResourceViewDimension.Texture1D:
                ref readonly var tex1d = ref descriptor.Texture1D;

                desc.Texture1D = new() {
                    MostDetailedMip = tex1d.MostDetailedMip,
                    MipLevels = tex1d.MipLevels,
                    ResourceMinLODClamp = 0,
                };
                break;

            case ResourceViewDimension.Texture1DArray:
                ref readonly var tex1darr = ref descriptor.Texture1DArray;

                desc.Texture1DArray = new() {
                    MostDetailedMip = tex1darr.MostDetailedMip,
                    MipLevels = tex1darr.MipLevels,
                    FirstArraySlice = tex1darr.FirstArraySlice,
                    ArraySize = tex1darr.ArraySize,
                    ResourceMinLODClamp = 0,
                };
                break;

            case ResourceViewDimension.Texture2D:
                ref readonly var tex2d = ref descriptor.Texture2D;

                desc.Texture2D = new() {
                    MostDetailedMip = tex2d.MostDetailedMip,
                    MipLevels = tex2d.MipLevels,
                    PlaneSlice = tex2d.PlaneSlice,
                };
                break;

            case ResourceViewDimension.Texture2DArray:
                ref readonly var tex2darr = ref descriptor.Texture2DArray;

                desc.Texture2DArray = new() {
                    MostDetailedMip = tex2darr.MostDetailedMip,
                    MipLevels = tex2darr.MipLevels,
                    FirstArraySlice = tex2darr.FirstArraySlice,
                    ArraySize = tex2darr.ArraySize,
                    PlaneSlice = tex2darr.PlaneSlice,
                };
                break;

            case ResourceViewDimension.Texture2DMS: break;

            case ResourceViewDimension.Texture2DMSArray:
                ref readonly var tex2dmsarr = ref descriptor.Texture2DMSArray;

                desc.Texture2DMSArray = new() {
                     FirstArraySlice = tex2dmsarr.FirstArraySlice,
                     ArraySize = tex2dmsarr.ArraySize,
                };
                break;

            case ResourceViewDimension.Texture3D:
                ref readonly var tex3d = ref descriptor.Texture3D;

                desc.Texture3D = new() {
                    MostDetailedMip = tex3d.MostDetailedMip,
                    MipLevels = tex3d.MipLevels,
                    ResourceMinLODClamp = 0,
                };
                break;

            case ResourceViewDimension.TextureCube:
                ref readonly var texcube = ref descriptor.TextureCube;

                desc.TextureCube = new() {
                    MostDetailedMip = texcube.MostDetailedMip,
                    MipLevels = texcube.MipLevels,
                };
                break;

            case ResourceViewDimension.TextureCubeArray:
                ref readonly var texcubearr = ref descriptor.TextureCubeArray;

                desc.TextureCubeArray = new() {
                    MostDetailedMip = texcubearr.MostDetailedMip,
                    MipLevels = texcubearr.MipLevels,
                    NumCubes = texcubearr.NumCubes,
                    First2DArrayFace = texcubearr.First2DArrayFace,
                    ResourceMinLODClamp = 0,
                };
                break;

            default: throw new UnreachableException();
        }

        context.Device->CreateShaderResourceView((ID3D12Resource*)resource.NativeResource.Handle, &desc, handle);
        
        NativeView = new(handle.Ptr);
        _refcount = 1;
    }

    protected override void Dispose() {
        NativeView = default;
    }
}