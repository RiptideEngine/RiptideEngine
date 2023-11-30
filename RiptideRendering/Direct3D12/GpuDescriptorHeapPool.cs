using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class GpuDescriptorHeapPool(D3D12RenderingContext context, DescriptorHeapType type, uint minimumDescriptors) : IDisposable {
    private readonly List<nint> _pool = [];
    private readonly Queue<RetiredHeap> _retiredHeaps = [];
    private readonly List<AvailableHeap> _availableHeaps = [];

    private readonly object _lock = new();
    
    public ID3D12DescriptorHeap* Request(uint minDescriptors, ulong fenceValue) {
        lock (_lock) {
            while (_retiredHeaps.TryPeek(out var retired)) {
                if (retired.FenceValue > fenceValue) break;

                _retiredHeaps.Dequeue();
                _availableHeaps.Add(new(retired.Heap));
            }
            
            for (int i = 0; i < _availableHeaps.Count; i++) {
                var available = _availableHeaps[i];

                if (available.NumDescriptors >= minDescriptors) {
                    _availableHeaps.RemoveAt(i);

                    return available.Heap;
                }
            }

            ID3D12DescriptorHeap* pHeap;
            var desc = new DescriptorHeapDesc {
                Flags = DescriptorHeapFlags.ShaderVisible,
                Type = type,
                NumDescriptors = uint.Max(minimumDescriptors, minDescriptors),
            };

            int hr = context.Device->CreateDescriptorHeap(&desc, SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(), (void**)&pHeap);
            Marshal.ThrowExceptionForHR(hr);
            
            Helper.SetName(pHeap, $"GpuDescriptorHeap {_pool.Count}");

            _pool.Add((nint)pHeap);
            
            context.Logger?.Log(LoggingType.Info, $"Direct3D12 - {nameof(GpuDescriptorHeapPool)}: Descriptor heap allocated (NumDescriptors = {desc.NumDescriptors}, Address = 0x{(nint)pHeap:X}, CPU Handle = 0x{(nint)pHeap->GetCPUDescriptorHandleForHeapStart().Ptr:X}).");

            return pHeap;
        }
    }

    public void Return(ID3D12DescriptorHeap* pHeap, ulong fenceValue) {
        lock (_lock) {
            _retiredHeaps.Enqueue(new(pHeap, fenceValue));
        }
    }

    public void Dispose() {
        lock (_lock) {
            foreach (var heap in _pool) {
                ((ID3D12DescriptorHeap*)heap)->Release();
            }

            _pool.Clear();
            _availableHeaps.Clear();
            _retiredHeaps.Clear();
        }
    }
    
    private readonly struct RetiredHeap(ID3D12DescriptorHeap* heap, ulong fenceValue) {
        public readonly ID3D12DescriptorHeap* Heap = heap;
        public readonly ulong FenceValue = fenceValue;
    }

    private readonly struct AvailableHeap(ID3D12DescriptorHeap* heap) {
        public readonly ID3D12DescriptorHeap* Heap = heap;
        public readonly uint NumDescriptors = heap->GetDesc().NumDescriptors;
    }
}