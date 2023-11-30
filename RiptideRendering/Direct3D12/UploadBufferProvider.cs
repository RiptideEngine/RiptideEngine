using RiptideRendering.Direct3D12.Allocators;
using Silk.NET.Direct3D12;

using D3D12Range = Silk.NET.Direct3D12.Range;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class UploadBufferProvider {
    private ID3D12Resource* pCurrentResource;
    private void* pMappedPtr;

    private RetainedRingAllocator _allocator;

    private readonly List<nint> _preservedResources = [];

    public void RequestResource(UploadBufferPool pool, ulong minimumSize, ulong fenceValue) {
        pCurrentResource = pool.Request(MathUtils.AlignUpwardPow2<ulong>(minimumSize, D3D12.DefaultResourcePlacementAlignment), fenceValue);
        HResult hr = pCurrentResource->Map(0, new D3D12Range(0, 0), ref pMappedPtr);
        Marshal.ThrowExceptionForHR(hr);

        _allocator = new();
        _allocator.Reset(pCurrentResource->GetDesc().Width);
    }

    public bool TryAllocate(ulong size, uint alignment, out AllocatedRegion region) {
        if (_allocator.TryAllocate(size, alignment, out var offset)) {
            Debug.Assert(offset % alignment == 0, "offset % alignment == 0");

            region = new(pCurrentResource, (byte*)pMappedPtr + offset, offset);
            return true;
        }

        region = default;
        return false;
    }
    
    public void PreserveCurrentResource() {
        Debug.Assert(pCurrentResource != null, "pCurrentResource != null");

        _preservedResources.Add((nint)pCurrentResource);
        
        pCurrentResource->Unmap(0, new D3D12Range(0, 0));
        pMappedPtr = null;
        pCurrentResource = null;
    }
    
    public void ReturnResources(UploadBufferPool pool, ulong fenceValue) {
        foreach (var preserve in _preservedResources) {
            pool.Return((ID3D12Resource*)preserve, fenceValue);
        }
        _preservedResources.Clear();
    }

    public readonly struct AllocatedRegion(ID3D12Resource* resource, byte* pointer, ulong offset) {
        public readonly ID3D12Resource* Resource = resource;
        public readonly byte* Pointer = pointer;
        public readonly ulong Offset = offset;
    }
}