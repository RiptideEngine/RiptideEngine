using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

unsafe partial class D3D12RenderingContext {
    private CpuDescriptorAllocator _cpuDescriptorAllocator;
    
    public UploadBufferPool UploadBufferPool { get; private set; }
    public StagingDescriptorHeapPool StagingResourceHeapPool { get; private set; }
    
    public GpuDescriptorHeapPool GraphicsResourceDescHeapPool { get; private set; }

    private void InitializeResources() {
        _cpuDescriptorAllocator = new(this);
        UploadBufferPool = new(this);
        StagingResourceHeapPool = new(this, DescriptorHeapType.CbvSrvUav);
        GraphicsResourceDescHeapPool = new(this, DescriptorHeapType.CbvSrvUav, 512);
    }

    private void DisposeResources() {
        GraphicsResourceDescHeapPool?.Dispose();
        StagingResourceHeapPool?.Dispose();
        UploadBufferPool?.Dispose();
        _cpuDescriptorAllocator?.Dispose();
    }

    public CpuDescriptorHandle AllocateCpuDescriptor(DescriptorHeapType type) {
        return _cpuDescriptorAllocator.Allocate(type);
    }
}