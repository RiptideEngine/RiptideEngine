namespace RiptideRendering.Direct3D12;

unsafe partial class DescriptorCommitter {
    private readonly List<nint> _finishedResourceStagingHeaps = [];
    private readonly List<nint> _finishedSamplerStagingHeaps = [];

    private readonly StagingDescriptorHeap _stagingResourceDescriptorHeap = new();
    private readonly StagingDescriptorHeap _stagingSamplerDescriptorHeap = new();

    public void ReturnStagingHeaps() {
        foreach (var heap in _finishedResourceStagingHeaps) {
            _context.StagingResourceHeapPool.Return((ID3D12DescriptorHeap*)heap);
        }
        foreach (var heap in _finishedSamplerStagingHeaps) {
            _context.StagingSamplerHeapPool.Return((ID3D12DescriptorHeap*)heap);
        }

        _finishedResourceStagingHeaps.Clear();
        _finishedSamplerStagingHeaps.Clear();

        _stagingResourceDescriptorHeap.Return(_context.StagingResourceHeapPool);
        _stagingSamplerDescriptorHeap.Return(_context.StagingSamplerHeapPool);
    }

    private CpuDescriptorHandle AllocStagingResourceDescriptor(uint numDescriptors) {
        var incrementSize = _context.Constants.ResourceViewDescIncrementSize;
        CpuDescriptorHandle handle;

        if (_stagingResourceDescriptorHeap.IsValid) {
            if (_stagingResourceDescriptorHeap.TryAllocate(numDescriptors, incrementSize, out handle)) {
                return handle;
            }

            _finishedResourceStagingHeaps.Add((nint)_stagingResourceDescriptorHeap.DetachHeap());
        }

        _stagingResourceDescriptorHeap.RequestHeap(_context.StagingResourceHeapPool, numDescriptors);
        bool alloc = _stagingResourceDescriptorHeap.TryAllocate(numDescriptors, incrementSize, out handle);
        Debug.Assert(alloc, "Allocation from newly requested heap failed.");

        return handle;
    }

    private CpuDescriptorHandle AllocStagingSamplerDescriptor(uint numDescriptors) {
        var incrementSize = _context.Constants.SamplerDescIncrementSize;
        CpuDescriptorHandle handle;

        if (_stagingSamplerDescriptorHeap.IsValid) {
            if (_stagingSamplerDescriptorHeap.TryAllocate(numDescriptors, incrementSize, out handle)) {
                return handle;
            }

            _finishedResourceStagingHeaps.Add((nint)_stagingSamplerDescriptorHeap.DetachHeap());
        }

        _stagingSamplerDescriptorHeap.RequestHeap(_context.StagingSamplerHeapPool, numDescriptors);
        bool alloc = _stagingSamplerDescriptorHeap.TryAllocate(numDescriptors, incrementSize, out handle);
        Debug.Assert(alloc, "Allocation from newly requested heap failed.");

        return handle;
    }

}