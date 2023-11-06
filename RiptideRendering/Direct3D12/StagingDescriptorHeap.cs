namespace RiptideRendering.Direct3D12;

internal sealed unsafe class StagingDescriptorHeapPool : IDisposable {
    private const uint MinimumSize = 128;

    private readonly struct PoolEntry {
        public readonly ID3D12DescriptorHeap* Heap;
        public readonly uint NumDescriptors;

        public PoolEntry(ID3D12DescriptorHeap* heap, uint numDescriptors) {
            Heap = heap;
            NumDescriptors = numDescriptors;
        }
    }

    private readonly List<nint> _pool;
    private readonly List<PoolEntry> _availables;

    private ID3D12Device* pDevice;
    private readonly object _lock;
    private readonly DescriptorHeapType _type;

    public StagingDescriptorHeapPool(ID3D12Device* pDevice, DescriptorHeapType type) {
        this.pDevice = pDevice;
        pDevice->AddRef();

        _type = type;
        _pool = new List<nint>();
        _availables = new();
        _lock = new();
    }

    public ID3D12DescriptorHeap* Request(out uint descriptorSize) => Request(MinimumSize, out descriptorSize);
    public ID3D12DescriptorHeap* Request(uint minimumSize, out uint descriptorSize) {
        lock (_lock) {
            for (int i = 0; i < _availables.Count; i++) {
                var entry = _availables[i];

                if (entry.NumDescriptors >= minimumSize) {
                    _availables.RemoveAt(i);
                    descriptorSize = entry.NumDescriptors;

                    return entry.Heap;
                }
            }

            uint numDescriptors = uint.Max(minimumSize, MinimumSize);
            ID3D12DescriptorHeap* pOutput;

            int hr = pDevice->CreateDescriptorHeap(new DescriptorHeapDesc() {
                NodeMask = 1,
                Type = _type,
                NumDescriptors = numDescriptors,
            }, SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(), (void**)&pOutput);
            Marshal.ThrowExceptionForHR(hr);

            _pool.Add((nint)pOutput);

            Console.WriteLine($"Direct3D12 - {GetType().Name}: Heap created (Ptr = 0x{(nint)pOutput:X8}, NumDescriptors = {numDescriptors}, CPU Handle = 0x{pOutput->GetCPUDescriptorHandleForHeapStart().Ptr:X8}).");

            descriptorSize = numDescriptors;
            return pOutput;
        }
    }

    public void Return(ID3D12DescriptorHeap* pHeap) {
        Debug.Assert(pHeap != null);

        lock (_lock) {
            _availables.Add(new(pHeap, pHeap->GetDesc().NumDescriptors));
        }
    }

    private void Dispose(bool disposing) {
        if (pDevice == null) return;

        if (disposing) { }

        lock (_lock) {
            foreach (var pointer in _pool) {
                ((ID3D12DescriptorHeap*)pointer)->Release();
            }
            _pool.Clear();
            _availables.Clear();
        }

        pDevice->Release(); pDevice = null;
    }

    ~StagingDescriptorHeapPool() {
        Dispose(disposing: false);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

internal unsafe struct StagingDescriptorHeapLinearAllocator {
    private ID3D12DescriptorHeap* pHeap;
    private uint _numDescriptors;
    private uint _allocIndex;
    private CpuDescriptorHandle _startHandle;

    public readonly uint AllocatedAmount => _allocIndex;
    public readonly uint NumDescriptors => _numDescriptors;

    public readonly bool IsValid => pHeap != null;

    public StagingDescriptorHeapLinearAllocator(StagingDescriptorHeapPool pool) {
        pHeap = pool.Request(out _numDescriptors);
        _startHandle = pHeap->GetCPUDescriptorHandleForHeapStart();
    }

    public StagingDescriptorHeapLinearAllocator(StagingDescriptorHeapPool pool, uint minimumSize) {
        pHeap = pool.Request(minimumSize, out _numDescriptors);
        _startHandle = pHeap->GetCPUDescriptorHandleForHeapStart();
    }

    public bool TryAllocate(uint numDescriptors, uint descriptorIncrementSize, out CpuDescriptorHandle handle) {
        if (_allocIndex + numDescriptors > _numDescriptors) {
            handle = D3D12Helper.UnknownCpuHandle;
            return false;
        }

        handle = Unsafe.BitCast<nuint, CpuDescriptorHandle>(_startHandle.Ptr + descriptorIncrementSize * _allocIndex);
        _allocIndex += numDescriptors;

        return true;
    }

    public readonly bool HasEnoughDescriptors(uint numDescriptors) {
        return _allocIndex + numDescriptors <= _numDescriptors;
    }

    public void Finish(StagingDescriptorHeapPool pool) {
        if (pHeap != null) {
            pool.Return(pHeap);
        }

        pHeap = null;
        _allocIndex = 0;
        _numDescriptors = 0;
        _startHandle = D3D12Helper.UnknownCpuHandle;
    }

    public ID3D12DescriptorHeap* DetachHeap() {
        var heap = pHeap;
        pHeap = null;
        _allocIndex = 0;
        _numDescriptors = 0;
        _startHandle = D3D12Helper.UnknownCpuHandle;

        return heap;
    }
}