namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12ShaderResourceView : ShaderResourceView {
    public CpuDescriptorHandle Handle { get; private set; }
    
    [SkipLocalsInit]
    public D3D12ShaderResourceView(D3D12RenderingContext context, GpuResource resource, ShaderResourceViewDescription desc) {
        Unsafe.SkipInit(out ShaderResourceViewDesc srvdesc);
        ID3D12Resource* pResource;

        srvdesc.Shader4ComponentMapping = D3D12Helper.DefaultShader4ComponentMapping;

        switch (desc.Dimension) {
            case ShaderResourceViewDimension.Buffer: {
                if (resource is not D3D12GpuBuffer d3d12buffer) throw new ArgumentException("Buffer object is expected.");
                pResource = (ID3D12Resource*)d3d12buffer.NativeResourceHandle;

                ref readonly var buffer = ref desc.Buffer;
                srvdesc.ViewDimension = SrvDimension.Buffer;
                srvdesc.Format = Format.FormatUnknown;

                srvdesc.Buffer = new() {
                    FirstElement = buffer.FirstElement,
                    NumElements = buffer.NumElements,
                    StructureByteStride = buffer.StructureSize,
                    Flags = buffer.IsRaw ? BufferSrvFlags.Raw : BufferSrvFlags.None,
                };
                break;
            }

            case ShaderResourceViewDimension.Texture1D: {
                if (resource is not D3D12GpuTexture d3d12texture) throw new ArgumentException("Texture object is expected.");
                pResource = (ID3D12Resource*)d3d12texture.NativeResourceHandle;

                bool convert = D3D12Convert.TryConvert(desc.Format, out srvdesc.Format);
                Debug.Assert(convert);

                ref readonly var tex1d = ref desc.Texture1D;

                srvdesc.ViewDimension = SrvDimension.Texture1D;
                srvdesc.Texture1D = new() {
                    MostDetailedMip = tex1d.MostDetailedMip,
                    MipLevels = tex1d.MipLevels,
                };
                break;
            }

            case ShaderResourceViewDimension.Texture1DArray: {
                if (resource is not D3D12GpuTexture d3d12texture) throw new ArgumentException("Texture object is expected.");
                pResource = (ID3D12Resource*)d3d12texture.NativeResourceHandle;
                
                bool convert = D3D12Convert.TryConvert(desc.Format, out srvdesc.Format);
                Debug.Assert(convert);
                
                ref readonly var tex1darr = ref desc.Texture1DArray;

                srvdesc.ViewDimension = SrvDimension.Texture1Darray;
                srvdesc.Texture1DArray = new() {
                    MostDetailedMip = tex1darr.MostDetailedMip,
                    MipLevels = tex1darr.MipLevels,
                    FirstArraySlice = tex1darr.FirstArraySlice,
                    ArraySize = tex1darr.ArraySize,
                    ResourceMinLODClamp = 0,
                };
                break;
            }

            case ShaderResourceViewDimension.Texture2D: {
                if (resource is not D3D12GpuTexture d3d12texture) throw new ArgumentException("Texture object is expected.");
                pResource = (ID3D12Resource*)d3d12texture.NativeResourceHandle;
                
                bool convert = D3D12Convert.TryConvert(desc.Format, out srvdesc.Format);
                Debug.Assert(convert);
                
                ref readonly var tex2d = ref desc.Texture2D;

                srvdesc.ViewDimension = SrvDimension.Texture2D;
                srvdesc.Texture2D = new() {
                    MostDetailedMip = tex2d.MostDetailedMip,
                    MipLevels = tex2d.MipLevels,
                    PlaneSlice = tex2d.PlaneSlice,
                };
                break;
            }

            case ShaderResourceViewDimension.Texture2DArray: {
                if (resource is not D3D12GpuTexture d3d12texture) throw new ArgumentException("Texture object is expected.");
                pResource = (ID3D12Resource*)d3d12texture.NativeResourceHandle;
                
                bool convert = D3D12Convert.TryConvert(desc.Format, out srvdesc.Format);
                Debug.Assert(convert);
                
                ref readonly var tex2darr = ref desc.Texture2DArray;

                srvdesc.ViewDimension = SrvDimension.Texture2Darray;
                srvdesc.Texture2DArray = new() {
                    MostDetailedMip = tex2darr.MostDetailedMip,
                    MipLevels = tex2darr.MipLevels,
                    FirstArraySlice = tex2darr.FirstArraySlice,
                    ArraySize = tex2darr.ArraySize,
                    PlaneSlice = tex2darr.PlaneSlice,
                };
                break;
            }

            // case ShaderResourceViewDimension.Texture2DMS: break;
            //
            // case ShaderResourceViewDimension.Texture2DMSArray:
            //     ref readonly var tex2dmsarr = ref descriptor.Texture2DMSArray;
            //
            //     desc.Texture2DMSArray = new() {
            //          FirstArraySlice = tex2dmsarr.FirstArraySlice,
            //          ArraySize = tex2dmsarr.ArraySize,
            //     };
            //     break;
            
            case ShaderResourceViewDimension.Texture3D: {
                if (resource is not D3D12GpuTexture d3d12texture) throw new ArgumentException("Texture object is expected.");
                pResource = (ID3D12Resource*)d3d12texture.NativeResourceHandle;
                
                bool convert = D3D12Convert.TryConvert(desc.Format, out srvdesc.Format);
                Debug.Assert(convert);
                
                ref readonly var tex3d = ref desc.Texture3D;

                srvdesc.ViewDimension = SrvDimension.Texture3D;
                srvdesc.Texture3D = new() {
                    MostDetailedMip = tex3d.MostDetailedMip,
                    MipLevels = tex3d.MipLevels,
                    ResourceMinLODClamp = 0,
                };
                break;
            }

            case ShaderResourceViewDimension.TextureCube: {
                if (resource is not D3D12GpuTexture d3d12texture) throw new ArgumentException("Texture object is expected.");
                pResource = (ID3D12Resource*)d3d12texture.NativeResourceHandle;
                
                bool convert = D3D12Convert.TryConvert(desc.Format, out srvdesc.Format);
                Debug.Assert(convert);
                
                ref readonly var texcube = ref desc.TextureCube;

                srvdesc.ViewDimension = SrvDimension.Texturecube;
                srvdesc.TextureCube = new() {
                    MostDetailedMip = texcube.MostDetailedMip,
                    MipLevels = texcube.MipLevels,
                };
                break;
            }

            case ShaderResourceViewDimension.TextureCubeArray: {
                if (resource is not D3D12GpuTexture d3d12texture) throw new ArgumentException("Texture object is expected.");
                pResource = (ID3D12Resource*)d3d12texture.NativeResourceHandle;
                
                bool convert = D3D12Convert.TryConvert(desc.Format, out srvdesc.Format);
                Debug.Assert(convert);
                
                ref readonly var texcubearr = ref desc.TextureCubeArray;

                srvdesc.ViewDimension = SrvDimension.Texturecubearray;
                srvdesc.TextureCubeArray = new() {
                    MostDetailedMip = texcubearr.MostDetailedMip,
                    MipLevels = texcubearr.MipLevels,
                    NumCubes = texcubearr.NumCubes,
                    First2DArrayFace = texcubearr.First2DArrayFace,
                    ResourceMinLODClamp = 0,
                };
                break;
            }

            default: throw new UnreachableException();
        }
        
        Handle = context.GetResourceDescriptorAllocator(DescriptorHeapType.CbvSrvUav).Allocate();
        context.Device->CreateShaderResourceView(pResource, &srvdesc, Handle);
        
        _refcount = 1;
    }

    protected override void Dispose() {
        Handle = default;
    }
}