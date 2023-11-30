using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class StagingDescriptorHeapPool(D3D12RenderingContext context, DescriptorHeapType type) : IDisposable {
    private const uint MinimumSize = 32;

    private readonly List<nint> _pool = [];
    private readonly List<AvailableHeap> _availables = [];

    private readonly object _lock = new();

    public ID3D12DescriptorHeap* Request(uint minimumSize) {
        lock (_lock) {
            for (int i = 0; i < _availables.Count; i++) {
                var available = _availables[i];

                if (available.NumDescriptors < minimumSize) continue;

                _availables.RemoveAt(i);
                return available.Heap;
            }

            DescriptorHeapDesc desc = new() {
                Type = type,
                NumDescriptors = uint.Max(minimumSize, MinimumSize),
                Flags = DescriptorHeapFlags.None,
            };

            ID3D12DescriptorHeap* pHeap;
            int hr = context.Device->CreateDescriptorHeap(&desc, SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(), (void**)&pHeap);
            Marshal.ThrowExceptionForHR(hr);

            Helper.SetName(pHeap, $"StagingDescriptorHeap {_pool.Count}");

            _pool.Add((nint)pHeap);

            context.Logger?.Log(LoggingType.Info, $"Direct3D12 - {nameof(StagingDescriptorHeapPool)}: Descriptor heap allocated (NumDescriptors = {desc.NumDescriptors}, Address = 0x{(nint)pHeap:X}, CPU Handle = 0x{(nint)pHeap->GetCPUDescriptorHandleForHeapStart().Ptr:X}). ");

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
        if (context == null) return;

        if (disposing) { }

        lock (_lock) {
            foreach (var pointer in _pool) {
                ((ID3D12DescriptorHeap*)pointer)->Release();
            }
            _pool.Clear();
            _availables.Clear();
        }

        context = null!;
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