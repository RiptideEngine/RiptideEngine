namespace RiptideRendering.Direct3D12;

internal unsafe class ResourceDescriptorAllocator : IDisposable {
    private const uint DefaultNumDescriptors = 256;

    private readonly List<nint> _heapPool;
    private ID3D12DescriptorHeap* pCurrentHeap;
    private CpuDescriptorHandle _currentHeapStartHandle;
    private uint _currentHeapAllocateIndex;

    private ID3D12Device* pDevice;
    private DescriptorHeapType _heapType;
    private readonly object _lock;
    private readonly uint _incrementSize;

    public ResourceDescriptorAllocator(ID3D12Device* pDevice, DescriptorHeapType heapType) {
        _heapType = heapType;
        this.pDevice = pDevice;
        pDevice->AddRef();
        _lock = new();

        _incrementSize = pDevice->GetDescriptorHandleIncrementSize(heapType);

        _heapPool = new();
    }

    public CpuDescriptorHandle Allocate(uint count) {
        lock (_lock) {
            if (pCurrentHeap == null || _currentHeapAllocateIndex + count >= DefaultNumDescriptors) {
                ID3D12DescriptorHeap* pCreatedHeap;
                int hr = pDevice->CreateDescriptorHeap(new DescriptorHeapDesc() {
                    Flags = DescriptorHeapFlags.None,
                    NumDescriptors = DefaultNumDescriptors,
                    NodeMask = 1,
                    Type = _heapType,
                }, SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(), (void**)&pCreatedHeap);
                Marshal.ThrowExceptionForHR(hr);

                _currentHeapAllocateIndex = 0;
                _currentHeapStartHandle = pCreatedHeap->GetCPUDescriptorHandleForHeapStart();
                _heapPool.Add((nint)pCreatedHeap);

                pCurrentHeap = pCreatedHeap;
            }

            var handle = _currentHeapStartHandle.Ptr + _incrementSize * _currentHeapAllocateIndex;
            _currentHeapAllocateIndex += count;

            return Unsafe.BitCast<nuint, CpuDescriptorHandle>(handle);
        }
    }

    protected virtual void Dispose(bool disposing) {
        if (pDevice == null) return;

        foreach (nint heap in _heapPool) {
            ((ID3D12DescriptorHeap*)heap)->Release();
        }

        pDevice->Release(); pDevice = null;
    }

    ~ResourceDescriptorAllocator() {
        Dispose(disposing: false);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}