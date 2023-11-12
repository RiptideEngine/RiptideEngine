namespace RiptideRendering.Direct3D12;

internal sealed unsafe class StagingDescriptorHeapPool : IDisposable {
    private const uint MinimumSize = 256;

    private readonly List<nint> _pool;
    private readonly List<AvailableHeap> _availables;

    private readonly object _lock;
    private readonly DescriptorHeapType _type;

    private D3D12RenderingContext _context;

    public StagingDescriptorHeapPool(D3D12RenderingContext context, DescriptorHeapType type) {
        _type = type;
        _pool = [];
        _availables = [];
        _lock = new();

        _context = context;
    }

    public ID3D12DescriptorHeap* Request(uint minimumSize) {
        lock (_lock) {
            for (int i = 0; i < _availables.Count; i++) {
                var available = _availables[i];

                if (available.NumDescriptors < minimumSize) continue;

                _availables.RemoveAt(i);
                return available.Heap;
            }

            DescriptorHeapDesc desc = new() {
                NodeMask = 1,
                Type = _type,
                NumDescriptors = uint.Max(minimumSize, MinimumSize),
                Flags = DescriptorHeapFlags.None,
            };

            ID3D12DescriptorHeap* pHeap;
            int hr = _context.Device->CreateDescriptorHeap(&desc, SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(), (void**)&pHeap);
            Marshal.ThrowExceptionForHR(hr);

            D3D12Helper.SetName(pHeap, $"StagingDescriptorHeap {_pool.Count}");

            _pool.Add((nint)pHeap);

            _context.Logger?.Log(LoggingType.Info, $"Direct3D12 - {nameof(StagingDescriptorHeap)}: Staging descriptor heap allocated (NumDescriptors = {desc.NumDescriptors}, Address = 0x{(nint)pHeap:X}, CPU Handle = 0x{(nint)pHeap->GetCPUDescriptorHandleForHeapStart().Ptr:X}). ");

            return pHeap;
        }
    }

    public void Return(ID3D12DescriptorHeap* pHeap) {
        Debug.Assert(pHeap != null);

        lock (_lock) {
            _availables.Add(new(pHeap));
        }
    }

    private void Dispose(bool disposing) {
        if (_context == null) return;

        if (disposing) { }

        lock (_lock) {
            foreach (var pointer in _pool) {
                ((ID3D12DescriptorHeap*)pointer)->Release();
            }
            _pool.Clear();
            _availables.Clear();
        }

        _context = null!;
    }

    ~StagingDescriptorHeapPool() {
        Dispose(disposing: false);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private readonly struct AvailableHeap(ID3D12DescriptorHeap* heap, uint numDescriptors) {
        public readonly ID3D12DescriptorHeap* Heap = heap;
        public readonly uint NumDescriptors = numDescriptors;

        public AvailableHeap(ID3D12DescriptorHeap* heap) : this(heap, heap->GetDesc().NumDescriptors) { }
    }
}

internal sealed unsafe class StagingDescriptorHeap {
    private ID3D12DescriptorHeap* pHeap;
    private uint _numDescriptors;
    private uint _allocated;
    private CpuDescriptorHandle _startHandle;

    private readonly object _lock = new();

    public bool IsValid => pHeap != null;

    public StagingDescriptorHeap() {
    }

    public void InitializeHeap(StagingDescriptorHeapPool pool, uint numDescriptors) {
        lock (_lock) {
            Debug.Assert(pHeap == null);

            pHeap = pool.Request(numDescriptors);
            _startHandle = pHeap->GetCPUDescriptorHandleForHeapStart();
            _numDescriptors = pHeap->GetDesc().NumDescriptors;
            _allocated = 0;
        }
    }

    public bool TryAllocate(uint numDescriptors, uint descriptorIncrementSize, out CpuDescriptorHandle handle) {
        lock (_lock) {
            if (_allocated + numDescriptors >= _numDescriptors) {
                handle = D3D12Helper.UnknownCpuHandle;

                return false;
            }

            handle = Unsafe.BitCast<nuint, CpuDescriptorHandle>(_startHandle.Ptr + descriptorIncrementSize * _allocated);
            _allocated += numDescriptors;

            return true;
        }
    }

    public void Return(StagingDescriptorHeapPool pool) {
        lock (_lock) {
            if (pHeap == null) return;

            pool.Return(pHeap);

            pHeap = null;
            _allocated = 0;
            _numDescriptors = 0;
            _startHandle = D3D12Helper.UnknownCpuHandle;
        }
    }

    public ID3D12DescriptorHeap* DetachHeap() {
        lock (_lock) {
            Debug.Assert(pHeap != null);

            var heap = pHeap;
            pHeap = null;
            _allocated = 0;
            _numDescriptors = 0;
            _startHandle = D3D12Helper.UnknownCpuHandle;

            return heap;
        }
    }

    public void RequestHeap(StagingDescriptorHeapPool pool, uint minimumDescriptors) {
        lock (_lock) {
            Debug.Assert(pHeap == null);

            pHeap = pool.Request(minimumDescriptors);
            _numDescriptors = pHeap->GetDesc().NumDescriptors;
            _startHandle = pHeap->GetCPUDescriptorHandleForHeapStart();
            _allocated = 0;
        }
    }
}