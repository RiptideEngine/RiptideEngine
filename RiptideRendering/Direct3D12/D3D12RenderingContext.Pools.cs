namespace RiptideRendering.Direct3D12;

internal partial class D3D12RenderingContext {
    public CommandListPool CommandListPool { get; private set; }
    public UploadBufferPool UploadBufferPool { get; private set; }
    public RootSignatureStorage RootSigStorage { get; private set; }

    private CpuDescriptorAllocator[] _resourceDescAlloc = [];

    public StagingDescriptorHeapPool StagingResourceHeapPool { get; private set; }
    public StagingDescriptorHeapPool StagingSamplerHeapPool { get; private set; }
    
    public GpuDescriptorHeapPool GpuResourceDescHeapPool { get; private set; }
    public GpuDescriptorHeap CurrentResourceGpuDescHeap { get; private set; }
    public GpuDescriptorHeapPool GpuSamplerDescHeapPool { get; private set; }
    public GpuDescriptorHeap CurrentSamplerGpuDescHeap { get; private set; }

    public readonly object GpuDescriptorSuballocationLock = new();

    //public void AllocateResourceGpuDescriptor(uint numDescriptors, out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle) {
    //    if (!CurrentResourceGpuDescHeap.TryAllocate(numDescriptors, out cpuHandle, out gpuHandle)) {
    //        CurrentResourceGpuDescHeap.Retire(GpuResourceDescHeapPool, RenderingQueue.NextFenceValue - 1);
    //        CurrentResourceGpuDescHeap.Request(GpuResourceDescHeapPool, numDescriptors);

    //        bool alloc = CurrentResourceGpuDescHeap.TryAllocate(numDescriptors, out cpuHandle, out gpuHandle);
    //        Debug.Assert(alloc);
    //    }
    //}

    //public void AllocateSamplerGpuDescriptor(uint numDescriptors, out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle) {
    //    if (!CurrentSamplerGpuDescHeap.TryAllocate(numDescriptors, out cpuHandle, out gpuHandle)) {
    //        CurrentSamplerGpuDescHeap.Retire(GpuSamplerDescHeapPool, RenderingQueue.NextFenceValue - 1);
    //        CurrentSamplerGpuDescHeap.Request(GpuSamplerDescHeapPool, numDescriptors);

    //        bool alloc = CurrentSamplerGpuDescHeap.TryAllocate(numDescriptors, out cpuHandle, out gpuHandle);
    //        Debug.Assert(alloc);
    //    }
    //}
}