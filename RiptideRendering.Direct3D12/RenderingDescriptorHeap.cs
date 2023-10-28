namespace RiptideRendering.Direct3D12;

internal sealed unsafe class RenderingDescriptorHeapPool {
    public const uint MinimumSize = 256;

    private readonly struct CacheEntry {
        public readonly ID3D12DescriptorHeap* Heap;
        public readonly uint NumDescriptors;

        public CacheEntry(ID3D12DescriptorHeap* pHeap) {
            Heap = pHeap;
            NumDescriptors = pHeap->GetDesc().NumDescriptors;
        }
    }
    private readonly struct RetiredEntry {
        public readonly ID3D12DescriptorHeap* Heap;
        public readonly ulong Fence;

        public RetiredEntry(ulong fence, ID3D12DescriptorHeap* pHeap) {
            Fence = fence;
            Heap = pHeap;
        }
    }

    private readonly List<nint> _pool;
    private readonly Queue<RetiredEntry> _retired;
    private readonly List<CacheEntry> _availables;

    private readonly object _lock;
    private readonly D3D12RenderingContext _ctx;
    private readonly DescriptorHeapType _type;

    public RenderingDescriptorHeapPool(D3D12RenderingContext context, DescriptorHeapType type) {
        _pool = new();
        _retired = new();
        _availables = new();

        _ctx = context;

        _type = type;
        _lock = new();
    }

    public ID3D12DescriptorHeap* Request() => Request(MinimumSize);

    public ID3D12DescriptorHeap* Request(uint minimumSize) {
        lock (_lock) {
            while (_retired.TryPeek(out var peeked) && _ctx.RenderingQueue.IsFenceCompleted(peeked.Fence)) {
                _availables.Add(new(_retired.Dequeue().Heap));
            }

            for (int i = _availables.Count - 1; i >= 0; i--) {
                var entry = _availables[i];

                if (entry.NumDescriptors >= minimumSize) {
                    _availables.RemoveAt(i);

                    return entry.Heap;
                }
            }

            ID3D12DescriptorHeap* pOutput;
            DescriptorHeapDesc desc = new() {
                NumDescriptors = uint.Max(MinimumSize, minimumSize),
                Type = _type,
                NodeMask = 1,
                Flags = DescriptorHeapFlags.ShaderVisible,
            };

            int hr = _ctx.Device->CreateDescriptorHeap(&desc, SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(), (void**)&pOutput);
            Marshal.ThrowExceptionForHR(hr);

            _pool.Add((nint)pOutput);

            Console.WriteLine($"Direct3D12 - {GetType().Name}: Heap created (Ptr = 0x{(nint)pOutput:X8}, NumDescriptors = {desc.NumDescriptors}, CPU Handle = 0x{pOutput->GetCPUDescriptorHandleForHeapStart().Ptr:X8}, GPU Handle = 0x{pOutput->GetGPUDescriptorHandleForHeapStart().Ptr:X8}).");

            return pOutput;
        }
    }

    public void Return(ulong fenceValue, ID3D12DescriptorHeap* pHeap) {
        lock (_lock) {
            _retired.Enqueue(new(fenceValue, pHeap));
        }
    }

    public void Dispose() {
        foreach (var pointer in _pool) {
            ((ID3D12DescriptorHeap*)pointer)->Release();
        }
        _pool.Clear();
        _availables.Clear();
        _retired.Clear();
    }
}

internal sealed unsafe class DynamicRenderingDescriptorHeap {
    private readonly List<nint> _retiredHeaps;

    private readonly RenderingDescriptorHeapPool _pool;
    private ID3D12DescriptorHeap* pCurrentHeap;
    private DescriptorHandle _firstHandle;
    private readonly uint _incrementSize;
    private uint currentHeapNumDescs;
    private uint _offset;

    private readonly DescriptorHeapType _heapType;

    public uint AllocatedAmount => _offset;

    public ID3D12DescriptorHeap* CurrentHeap => pCurrentHeap;

    public DynamicRenderingDescriptorHeap(D3D12RenderingContext context, DescriptorHeapType type) {
        Debug.Assert(type is DescriptorHeapType.CbvSrvUav or DescriptorHeapType.Sampler);

        _incrementSize = context.Device->GetDescriptorHandleIncrementSize(type);

        _pool = context.RenderingResourceDescHeapPool;
        _heapType = type;

        _retiredHeaps = new();
    }

    public void CleanUp(ulong fenceValue) {
        RetireCurrentHeap();
        RetiredUsedHeap(fenceValue);
    }

    private void RetiredUsedHeap(ulong fenceValue) {
        foreach (var ptr in _retiredHeaps) {
            _pool.Return(fenceValue, (ID3D12DescriptorHeap*)ptr);
        }
        _retiredHeaps.Clear();
    }

    public void RetireCurrentHeap() {
        if (_offset == 0 || pCurrentHeap == null) return;

        _retiredHeaps.Add((nint)pCurrentHeap);
        pCurrentHeap = null;
        _offset = 0;
        currentHeapNumDescs = 0;
    }

    public void EnsurePrepared() => EnsurePrepared(RenderingDescriptorHeapPool.MinimumSize);

    public void EnsurePrepared(uint minimumCount) {
        if (currentHeapNumDescs >= minimumCount) return;

        RetireCurrentHeap();

        pCurrentHeap = _pool.Request(minimumCount);
        currentHeapNumDescs = pCurrentHeap->GetDesc().NumDescriptors;

        _firstHandle = new(pCurrentHeap->GetCPUDescriptorHandleForHeapStart(), pCurrentHeap->GetGPUDescriptorHandleForHeapStart());
    }

    public DescriptorHandle Allocate(uint count) {
        if (pCurrentHeap == null) {
            pCurrentHeap = _pool.Request(count);
            currentHeapNumDescs = pCurrentHeap->GetDesc().NumDescriptors;

            _firstHandle = new(pCurrentHeap->GetCPUDescriptorHandleForHeapStart(), pCurrentHeap->GetGPUDescriptorHandleForHeapStart());
        }

        uint offset = _incrementSize * _offset;
        var oldOffset = _offset;
        _offset += count;

        return new(_firstHandle.Cpu.Ptr + offset, _firstHandle.Gpu.Ptr + offset);
    }

    public bool HasEnoughDescriptor(uint count) {
        return _offset + count < currentHeapNumDescs;
    }
}