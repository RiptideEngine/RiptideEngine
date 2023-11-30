using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class CpuDescriptorAllocator : IDisposable {
    private readonly List<nint> _finishedHeaps;

    private Allocator[] _allocators;
    private readonly D3D12RenderingContext _context;
    private readonly object[] _locks;

    public CpuDescriptorAllocator(D3D12RenderingContext context) {
        _context = context;

        _finishedHeaps = [];

        _allocators = new Allocator[(int)DescriptorHeapType.NumTypes];
        _locks = new object[(int)DescriptorHeapType.NumTypes];
        
        _allocators[0] = new(context, DescriptorHeapType.CbvSrvUav, 256);
        _allocators[1] = new(context, DescriptorHeapType.Sampler, 256);
        _allocators[2] = new(context, DescriptorHeapType.Rtv, 256);
        _allocators[3] = new(context, DescriptorHeapType.Dsv, 256);

        _locks[0] = new();
        _locks[1] = new();
        _locks[2] = new();
        _locks[3] = new();
    }

    public CpuDescriptorHandle Allocate(DescriptorHeapType type) {
        Debug.Assert(type is >= DescriptorHeapType.CbvSrvUav and <= DescriptorHeapType.Dsv);

        lock (_locks[(int)type]) {
            ref var allocator = ref _allocators[(int)type];

            if (allocator.TryAllocate(out var handle)) return handle;
            
            _finishedHeaps.Add((nint)allocator.Heap.Detach());
            allocator = new(_context, type, 256);

            bool alloc = allocator.TryAllocate(out handle);
            Debug.Assert(alloc);

            return handle;
        }
    }

    public void Dispose() {
        foreach (var heap in _finishedHeaps) {
            ((ID3D12DescriptorHeap*)heap)->Release();
        }
        _finishedHeaps.Clear();
        _allocators[0].Heap.Release();
        _allocators[1].Heap.Release();
        _allocators[2].Heap.Release();
        _allocators[3].Heap.Release();

        _allocators = [];
    }

    private struct Allocator {
        public ComPtr<ID3D12DescriptorHeap> Heap;
        private CpuDescriptorHandle StartHandle;
        private CpuDescriptorHandle EndHandle;
        private uint _incrementSize;
        
        public Allocator(D3D12RenderingContext context, DescriptorHeapType type, uint numDescriptors) {
            HResult hr = context.Device->CreateDescriptorHeap(new DescriptorHeapDesc {
                Type = type,
                NumDescriptors = numDescriptors,
            }, SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(), (void**)Heap.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            _incrementSize = context.Device->GetDescriptorHandleIncrementSize(type);
            StartHandle = Heap.GetCPUDescriptorHandleForHeapStart();
            EndHandle = new() { Ptr = StartHandle.Ptr + _incrementSize * numDescriptors };
        }

        public bool TryAllocate(out CpuDescriptorHandle handle) {
            if (StartHandle.Ptr >= EndHandle.Ptr) {
                handle = default;
                return false;
            }

            handle = StartHandle;
            StartHandle.Ptr += _incrementSize;
            return true;
        }
    }
}