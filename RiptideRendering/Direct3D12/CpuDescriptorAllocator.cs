namespace RiptideRendering.Direct3D12;

/// <summary>
/// Descriptor allocator class that used for creating Resource Views.
/// </summary>
internal sealed unsafe class CpuDescriptorAllocator : IDisposable {
    private const uint DefaultNumDescriptors = 128;

    private readonly List<nint> _pool;
    private ID3D12DescriptorHeap* pCurrentHeap;
    private CpuDescriptorHandle _heapStart;
    private uint _suballocateIndex;

    private D3D12RenderingContext _context;
    private readonly DescriptorHeapType _heapType;
    private readonly uint _incrementSize;

    private readonly object _lock = new();

    public CpuDescriptorAllocator(D3D12RenderingContext context, DescriptorHeapType type) {
        _context = context;
        _incrementSize = context.Device->GetDescriptorHandleIncrementSize(type);
        _heapType = type;
        _pool = new();

        ID3D12DescriptorHeap* pOutput;
        CreateHeap(&pOutput);

        D3D12Helper.SetName(pOutput, $"CpuDescriptorAllocator 0");
        _pool.Add((nint)pOutput);

        _heapStart = pOutput->GetCPUDescriptorHandleForHeapStart();
        _suballocateIndex = 0;

        pCurrentHeap = pOutput;
    }

    public CpuDescriptorHandle Allocate() {
        lock (_lock) {
            if (_suballocateIndex >= DefaultNumDescriptors) {
                ID3D12DescriptorHeap* pOutput;
                CreateHeap(&pOutput);

                D3D12Helper.SetName(pOutput, $"CpuDescriptorAllocator {_pool.Count}");

                _pool.Add((nint)pOutput);

                _heapStart = pOutput->GetCPUDescriptorHandleForHeapStart();
                _suballocateIndex = 0;

                pCurrentHeap = pOutput;
            }

            var outputHandle = _heapStart;

            outputHandle.Offset(_suballocateIndex, _incrementSize);
            _suballocateIndex++;

            return outputHandle;
        }
    }

    private void CreateHeap(ID3D12DescriptorHeap** ppOutput) {
        int hr = _context.Device->CreateDescriptorHeap(new DescriptorHeapDesc() {
            Type = _heapType,
            NumDescriptors = DefaultNumDescriptors,
            NodeMask = 1,
            Flags = DescriptorHeapFlags.None,
        }, SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(), (void**)ppOutput);
        Marshal.ThrowExceptionForHR(hr);

        _context.Logger?.Log(LoggingType.Info, $"Direct3D12 - {nameof(CpuDescriptorAllocator)}: Descriptor heap type {SilkHelper.GetNativeName(_heapType, "Name")["D3D12_DESCRIPTOR_HEAP_TYPE_".Length..]} created.");
    }

    private void Dispose(bool disposing) {
        if (_context == null) return;

        foreach (var heap in _pool) {
            ((ID3D12DescriptorHeap*)heap)->Release();
        }
        pCurrentHeap = null!;
        _suballocateIndex = 0;
        _heapStart = D3D12Helper.UnknownCpuHandle;

        _context = null!;
    }

    ~CpuDescriptorAllocator() {
        Dispose(disposing: false);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}