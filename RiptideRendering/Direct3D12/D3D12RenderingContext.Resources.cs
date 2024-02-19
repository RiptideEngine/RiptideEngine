using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

unsafe partial class D3D12RenderingContext {
    private CpuDescriptorAllocator _cpuDescriptorAllocator = null!;
    
    public UploadBufferPool UploadBufferPool { get; private set; } = null!;
    public StagingDescriptorHeapPool StagingResourceHeapPool { get; private set; } = null!;
    
    public GpuDescriptorHeapPool ResourceDescriptorHeapPool { get; private set; } = null!;

    private void InitializeResources() {
        _cpuDescriptorAllocator = new(this);
        UploadBufferPool = new(this);
        StagingResourceHeapPool = new(this, DescriptorHeapType.CbvSrvUav);
        ResourceDescriptorHeapPool = new(this, DescriptorHeapType.CbvSrvUav, 4096);
    }

    private void DisposeResources() {
        ResourceDescriptorHeapPool?.Dispose();
        StagingResourceHeapPool?.Dispose();
        UploadBufferPool?.Dispose();
        _cpuDescriptorAllocator?.Dispose();
    }

    public CpuDescriptorHandle AllocateCpuDescriptor(DescriptorHeapType type) {
        return _cpuDescriptorAllocator.Allocate(type);
    }
}