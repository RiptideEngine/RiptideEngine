using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12UnorderedAccessView : UnorderedAccessView {
    public CpuDescriptorHandle Handle { get; private set; }

    public D3D12UnorderedAccessView(D3D12RenderingContext context, GpuResource resource, in UnorderedAccessViewDescription desc) {
        Unsafe.SkipInit(out UnorderedAccessViewDesc uavdesc);
        ID3D12Resource* pResource;

        switch (desc.Dimension) {
            case UnorderedAccessViewDimension.Buffer: {
                if (resource is not D3D12GpuBuffer d3d12buffer) throw new ArgumentException("Buffer object is expected.");
                pResource = (ID3D12Resource*)d3d12buffer.NativeResourceHandle;

                ref readonly var buffer = ref desc.Buffer;
                uavdesc.ViewDimension = UavDimension.Buffer;
                uavdesc.Format = Format.FormatUnknown;

                uavdesc.Buffer = new() {
                    FirstElement = buffer.FirstElement,
                    NumElements = buffer.NumElements,
                    StructureByteStride = buffer.StructureSize,
                    Flags = buffer.IsRaw ? BufferUavFlags.Raw : BufferUavFlags.None,
                };
                break;
            }

            case UnorderedAccessViewDimension.Texture1D: {
                if (resource is not D3D12GpuTexture d3d12texture) throw new ArgumentException("Texture object is expected.");
                pResource = (ID3D12Resource*)d3d12texture.NativeResourceHandle;

                bool convert = Converting.TryConvert(desc.Format, out uavdesc.Format);
                Debug.Assert(convert);

                ref readonly var tex1d = ref desc.Texture1D;

                uavdesc.ViewDimension = UavDimension.Texture1D;
                uavdesc.Texture1D = new() {
                    MipSlice = tex1d.MipSlice,
                };
                break;
            }

            case UnorderedAccessViewDimension.Texture1DArray: {
                if (resource is not D3D12GpuTexture d3d12texture) throw new ArgumentException("Texture object is expected.");
                pResource = (ID3D12Resource*)d3d12texture.NativeResourceHandle;
                
                bool convert = Converting.TryConvert(desc.Format, out uavdesc.Format);
                Debug.Assert(convert);
                
                ref readonly var tex1darr = ref desc.Texture1DArray;

                uavdesc.ViewDimension = UavDimension.Texture1Darray;
                uavdesc.Texture1DArray = new() {
                    MipSlice = tex1darr.MipSlice,
                    FirstArraySlice = tex1darr.FirstArraySlice,
                    ArraySize = tex1darr.ArraySize,
                };
                break;
            }

            case UnorderedAccessViewDimension.Texture2D: {
                if (resource is not D3D12GpuTexture d3d12texture) throw new ArgumentException("Texture object is expected.");
                pResource = (ID3D12Resource*)d3d12texture.NativeResourceHandle;
                
                bool convert = Converting.TryConvert(desc.Format, out uavdesc.Format);
                Debug.Assert(convert);
                
                ref readonly var tex2d = ref desc.Texture2D;

                uavdesc.ViewDimension = UavDimension.Texture2D;
                uavdesc.Texture2D = new() {
                    MipSlice = tex2d.MipSlice,
                    PlaneSlice = tex2d.PlaneSlice,
                };
                break;
            }

            case UnorderedAccessViewDimension.Texture2DArray: {
                if (resource is not D3D12GpuTexture d3d12texture) throw new ArgumentException("Texture object is expected.");
                pResource = (ID3D12Resource*)d3d12texture.NativeResourceHandle;
                
                bool convert = Converting.TryConvert(desc.Format, out uavdesc.Format);
                Debug.Assert(convert);
                
                ref readonly var tex2darr = ref desc.Texture2DArray;

                uavdesc.ViewDimension = UavDimension.Texture2Darray;
                uavdesc.Texture2DArray = new() {
                    MipSlice = tex2darr.MipSlice,
                    FirstArraySlice = tex2darr.FirstArraySlice,
                    ArraySize = tex2darr.ArraySize,
                    PlaneSlice = tex2darr.PlaneSlice,
                };
                break;
            }

            case UnorderedAccessViewDimension.Texture3D: {
                if (resource is not D3D12GpuTexture d3d12texture) throw new ArgumentException("Texture object is expected.");
                pResource = (ID3D12Resource*)d3d12texture.NativeResourceHandle;
                
                bool convert = Converting.TryConvert(desc.Format, out uavdesc.Format);
                Debug.Assert(convert);
                
                ref readonly var tex3d = ref desc.Texture3D;

                uavdesc.ViewDimension = UavDimension.Texture3D;
                uavdesc.Texture3D = new() {
                    MipSlice = tex3d.MipSlice,
                    FirstWSlice = tex3d.FirstWSlice,
                    WSize = tex3d.WSize,
                };
                break;
            }

            default: throw new UnreachableException();
        }

        Handle = context.AllocateCpuDescriptor(DescriptorHeapType.CbvSrvUav);
        context.Device->CreateUnorderedAccessView(pResource, null, &uavdesc, Handle);
        
        _refcount = 1;
    }
    
    protected override void Dispose() {
        Handle = default;
    }
}