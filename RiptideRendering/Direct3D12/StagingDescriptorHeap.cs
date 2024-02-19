using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class StagingDescriptorHeapPool : IDisposable {
    private const uint NumDescriptorsPerPage = 1 << 12;
    
    private readonly object _lock = new();

    private DescriptorPage _currentPage;
    private uint _pagePointer;

    private readonly List<nint> _heaps;
    private readonly List<DescriptorPage> _retiredPages;
    private readonly Stack<nint> _readyHeaps;

    private readonly DescriptorHeapType _type;
    private D3D12RenderingContext _context;
    
    public StagingDescriptorHeapPool(D3D12RenderingContext context, DescriptorHeapType type) {
        ID3D12DescriptorHeap* pHeap;
        int hr = context.Device->CreateDescriptorHeap(new DescriptorHeapDesc {
            NumDescriptors = NumDescriptorsPerPage, // I put random amount here for no reason
            Flags = DescriptorHeapFlags.None,
            NodeMask = 1,
            Type = type,
        }, SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(), (void**)&pHeap);
        Marshal.ThrowExceptionForHR(hr);
        
        Helper.SetName(pHeap, "StagingDescriptorHeap");

        _currentPage = new(pHeap);
        _retiredPages = [];
        _readyHeaps = [];
        _heaps = [(nint)pHeap];

        _context = context;
        _type = type;
    }
    
    public CpuDescriptorHandle Allocate(uint numDescriptors) {
        Debug.Assert(numDescriptors <= NumDescriptorsPerPage, "numDescriptors <= NumDescriptorsPerPage");
        
        lock (_lock) {
            if (_pagePointer + numDescriptors > NumDescriptorsPerPage) {
                _retiredPages.Add(_currentPage);

                if (_readyHeaps.TryPop(out var pop)) {
                    _currentPage = new((ID3D12DescriptorHeap*)pop);
                } else {
                    ID3D12DescriptorHeap* pHeap;
                    int hr = _context.Device->CreateDescriptorHeap(new DescriptorHeapDesc {
                        NumDescriptors = NumDescriptorsPerPage, // I put random amount here for no reason
                        Flags = DescriptorHeapFlags.None,
                        NodeMask = 1,
                        Type = _type,
                    }, SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(), (void**)&pHeap);
                    Marshal.ThrowExceptionForHR(hr);

                    Helper.SetName(pHeap, "StagingDescriptorHeap");

                    _currentPage = new(pHeap);
                    _heaps.Add((nint)pHeap);
                }
                
                _pagePointer = 0;
            }

            var handle = _currentPage.Heap->GetCPUDescriptorHandleForHeapStart();
            handle.Ptr += _context.Device->GetDescriptorHandleIncrementSize(_type) * _pagePointer;
            
            _currentPage.SuballocatedAmount++;

            return handle;
        }
    }

    public void Deallocate(CpuDescriptorHandle handle) {
        var increment = _context.Device->GetDescriptorHandleIncrementSize(_type);

        lock (_lock) {
            var startHandle = _currentPage.Heap->GetCPUDescriptorHandleForHeapStart();

            if (handle.Ptr >= startHandle.Ptr && handle.Ptr < startHandle.Ptr + NumDescriptorsPerPage * increment) {
                Debug.Assert(_currentPage.SuballocatedAmount != 0, "_currentPage.SuballocatedAmount != 0");
                
                _currentPage.SuballocatedAmount--;
                return;
            }

            int idx = 0;
            foreach (ref var page in CollectionsMarshal.AsSpan(_retiredPages)) {
                startHandle = page.Heap->GetCPUDescriptorHandleForHeapStart();
                
                if (handle.Ptr >= startHandle.Ptr && handle.Ptr < startHandle.Ptr + NumDescriptorsPerPage * increment) {
                    page.SuballocatedAmount--;
            
                    if (--page.SuballocatedAmount == 0) {
                        _readyHeaps.Push((nint)page.Heap);
                        _retiredPages.RemoveAt(idx);
                    }
                    return;
                }

                idx++;
            }
        }
    }
    
    private void Dispose(bool disposing) {
        if (_context == null) return;
    
        if (disposing) { }
    
        lock (_lock) {
            foreach (var pointer in _heaps) {
                ((ID3D12DescriptorHeap*)pointer)->Release();
            }
            _heaps.Clear();
            _readyHeaps.Clear();
            _retiredPages.Clear();
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

    private struct DescriptorPage(ID3D12DescriptorHeap* heap) {
        public ID3D12DescriptorHeap* Heap = heap;
        public uint SuballocatedAmount = 0;
    }

    // private const uint MinimumSize = 32;
    //
    // private readonly List<nint> _pool = [];
    // private readonly List<AvailableHeap> _availables = [];
    //
    // private readonly object _lock = new();
    //
    // public ID3D12DescriptorHeap* Request(uint minimumSize) {
    //     lock (_lock) {
    //         for (int i = 0; i < _availables.Count; i++) {
    //             var available = _availables[i];
    //
    //             if (available.NumDescriptors < minimumSize) continue;
    //
    //             _availables.RemoveAt(i);
    //             return available.Heap;
    //         }
    //
    //         DescriptorHeapDesc desc = new() {
    //             Type = type,
    //             NumDescriptors = uint.Max(minimumSize, MinimumSize),
    //             Flags = DescriptorHeapFlags.None,
    //         };
    //
    //         ID3D12DescriptorHeap* pHeap;
    //         int hr = context.Device->CreateDescriptorHeap(&desc, SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(), (void**)&pHeap);
    //         Marshal.ThrowExceptionForHR(hr);
    //
    //         Helper.SetName(pHeap, $"StagingDescriptorHeap {_pool.Count}");
    //
    //         _pool.Add((nint)pHeap);
    //
    //         context.Logger?.Log(LoggingType.Info, $"Direct3D12 - {nameof(StagingDescriptorHeapPool)}: Descriptor heap allocated (NumDescriptors = {desc.NumDescriptors}, Address = 0x{(nint)pHeap:X}, CPU Handle = 0x{(nint)pHeap->GetCPUDescriptorHandleForHeapStart().Ptr:X}). ");
    //
    //         return pHeap;
    //     }
    // }
    //
    // public void Return(ID3D12DescriptorHeap* pHeap) {
    //     Debug.Assert(pHeap != null);
    //
    //     lock (_lock) {
    //         _availables.Add(new(pHeap));
    //     }
    // }
    //
    // private void Dispose(bool disposing) {
    //     if (context == null) return;
    //
    //     if (disposing) { }
    //
    //     lock (_lock) {
    //         foreach (var pointer in _pool) {
    //             ((ID3D12DescriptorHeap*)pointer)->Release();
    //         }
    //         _pool.Clear();
    //         _availables.Clear();
    //     }
    //
    //     context = null!;
    // }
    //
    // ~StagingDescriptorHeapPool() {
    //     Dispose(disposing: false);
    // }
    //
    // public void Dispose() {
    //     Dispose(disposing: true);
    //     GC.SuppressFinalize(this);
    // }
    //
    // private readonly struct AvailableHeap(ID3D12DescriptorHeap* heap, uint numDescriptors) {
    //     public readonly ID3D12DescriptorHeap* Heap = heap;
    //     public readonly uint NumDescriptors = numDescriptors;
    //
    //     public AvailableHeap(ID3D12DescriptorHeap* heap) : this(heap, heap->GetDesc().NumDescriptors) { }
    // }
}