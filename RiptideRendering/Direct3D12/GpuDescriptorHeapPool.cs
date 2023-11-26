namespace RiptideRendering.Direct3D12;

internal sealed unsafe class GpuDescriptorHeapPool(D3D12RenderingContext context, DescriptorHeapType type, uint minimumDescriptor) : IDisposable {
    private readonly List<nint> _pool = [];
    private readonly Queue<RetiredHeap> _retiredHeaps = [];
    private readonly List<AvailableHeap> _availableHeaps = [];

    private readonly object _lock = new();

    private readonly DescriptorHeapType _heapType = type;

    private D3D12RenderingContext _context = context;

    public ID3D12DescriptorHeap* Request() => Request(minimumDescriptor);

    public ID3D12DescriptorHeap* Request(uint minimumDescriptors) {
        lock (_lock) {
            for (int i = 0; i < _availableHeaps.Count; i++) {
                var available = _availableHeaps[i];

                if (available.NumDescriptors >= minimumDescriptors) {
                    _availableHeaps.RemoveAt(i);

                    return available.DescriptorHeap;
                }
            }

            ID3D12DescriptorHeap* pHeap;
            var desc = new DescriptorHeapDesc() {
                Flags = DescriptorHeapFlags.ShaderVisible,
                NodeMask = 1,
                Type = _heapType,
                NumDescriptors = uint.Max(minimumDescriptors, minimumDescriptor),
            };

            int hr = _context.Device->CreateDescriptorHeap(&desc, SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(), (void**)&pHeap);
            Marshal.ThrowExceptionForHR(hr);

            D3D12Helper.SetName(pHeap, $"GpuDescriptorHeap {_pool.Count}");
            _context.Logger?.Log(LoggingType.Info, $"Direct3D12 - {nameof(GpuDescriptorHeapPool)}: Descriptor heap allocated (NumDescriptors = {desc.NumDescriptors}, Address = 0x{(nint)pHeap:X}, CPU Handle = 0x{(nint)pHeap->GetCPUDescriptorHandleForHeapStart().Ptr:X}, GPU Handle = 0x{(nint)pHeap->GetGPUDescriptorHandleForHeapStart().Ptr:X}).");

            _pool.Add((nint)pHeap);

            return pHeap;
        }
    }

    public void Return(ID3D12DescriptorHeap* pHeap, ulong frameCount) {
        _retiredHeaps.Enqueue(new(pHeap, frameCount));
    }

    public void FinalizeRetirement(ulong completedFrame) {
        while (_retiredHeaps.TryPeek(out var retired)) {
            if (retired.FenceValue > completedFrame) continue;

            _retiredHeaps.Dequeue();

            _availableHeaps.Add(new(retired.DescriptorHeap));
        }
    }

    private void Dispose(bool disposing) {
        if (_context == null) return;

        if (disposing) { }

        lock (_lock) {
            foreach (var heap in _pool) {
                ((ID3D12DescriptorHeap*)heap)->Release();
            }

            _pool.Clear();
            _availableHeaps.Clear();
            _retiredHeaps.Clear();
        }

        _context = null!;
    }

    ~GpuDescriptorHeapPool() {
        Dispose(disposing: false);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private readonly struct RetiredHeap(ID3D12DescriptorHeap* heap, ulong fenceValue) {
        public readonly ID3D12DescriptorHeap* DescriptorHeap = heap;
        public readonly ulong FenceValue = fenceValue;
    }

    private readonly struct AvailableHeap(ID3D12DescriptorHeap* heap, uint numDescriptors) {
        public readonly ID3D12DescriptorHeap* DescriptorHeap = heap;
        public readonly uint NumDescriptors = numDescriptors;

        public AvailableHeap(ID3D12DescriptorHeap* heap) : this(heap, heap->GetDesc().NumDescriptors) { }
    }
}

internal sealed unsafe class GpuDescriptorHeap {
    private ID3D12DescriptorHeap* pCurrentHeap;
    private uint _allocated;
    private uint _numDescriptors;

    private CpuDescriptorHandle _startCpuHandle;
    private GpuDescriptorHandle _startGpuHandle;

    private readonly uint _incrementSize;

    public ID3D12DescriptorHeap* Heap => pCurrentHeap;
    public uint AllocatedAmount => _allocated;
    public uint NumDescriptors => _numDescriptors;

    public GpuDescriptorHeap(GpuDescriptorHeapPool pool, uint incrementSize) {
        var heap = pool.Request();
        Debug.Assert(heap != null);

        pCurrentHeap = heap;
        _allocated = 0;

        _incrementSize = incrementSize;

        _numDescriptors = pCurrentHeap->GetDesc().NumDescriptors;
        _startCpuHandle = pCurrentHeap->GetCPUDescriptorHandleForHeapStart();
        _startGpuHandle = pCurrentHeap->GetGPUDescriptorHandleForHeapStart();
    }

    public bool TryAllocate(uint amount, out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle) {
        if (_allocated + amount >= _numDescriptors) {
            cpuHandle = D3D12Helper.UnknownCpuHandle;
            gpuHandle = D3D12Helper.UnknownGpuHandle;
            return false;
        }

        uint offset = _allocated * _incrementSize;

        cpuHandle = new() { Ptr = _startCpuHandle.Ptr + offset };
        gpuHandle = new() { Ptr = _startGpuHandle.Ptr + offset };

        _allocated += amount;

        return true;
    }

    public void Retire(GpuDescriptorHeapPool pool, ulong frameCount) {
        Debug.Assert(pCurrentHeap != null);

        pool.Return(pCurrentHeap, frameCount);
        pCurrentHeap = null;
        _allocated = 0;

        _startCpuHandle = D3D12Helper.UnknownCpuHandle;
        _startGpuHandle = D3D12Helper.UnknownGpuHandle;
    }

    public void Request(GpuDescriptorHeapPool pool, uint numDescriptors) {
        var heap = pool.Request(numDescriptors);
        Debug.Assert(heap != null);

        pCurrentHeap = heap;

        _numDescriptors = pCurrentHeap->GetDesc().NumDescriptors;
        _startCpuHandle = pCurrentHeap->GetCPUDescriptorHandleForHeapStart();
        _startGpuHandle = pCurrentHeap->GetGPUDescriptorHandleForHeapStart();
    }
}