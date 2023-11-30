using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class CommandAllocatorPool(D3D12RenderingContext context, CommandListType type) : IDisposable {
    private readonly List<nint> _allocators = [];
    private readonly Queue<RetiredAllocator> _retiredAllocators = [];
    private readonly Stack<nint> _availableAllocators = [];

    private readonly object _lock = new();

    public ID3D12CommandAllocator* Request(ulong completedFenceValue) {
        lock (_lock) {
            ID3D12CommandAllocator* allocator;
            HResult hr;
            
            while (_retiredAllocators.TryPeek(out var retired)) {
                if (completedFenceValue >= retired.CompleteFenceValue) {
                    allocator = _retiredAllocators.Dequeue().Allocator;
                    hr = allocator->Reset();
                    Marshal.ThrowExceptionForHR(hr);
                    
                    _availableAllocators.Push((nint)allocator);
                } else break;
            }

            if (_availableAllocators.TryPop(out var pop)) {
                return (ID3D12CommandAllocator*)pop;
            }

            hr = context.Device->CreateCommandAllocator(type, SilkMarshal.GuidPtrOf<ID3D12CommandAllocator>(), (void**)&allocator);
            Marshal.ThrowExceptionForHR(hr);

            _allocators.Add((nint)allocator);

            return allocator;
        }
    }

    public void Return(ID3D12CommandAllocator* allocator, ulong fenceValue) {
        lock (_lock) {
            _retiredAllocators.Enqueue(new(allocator, fenceValue));
        }
    }
    
    public void Dispose() {
        lock (_lock) {
            foreach (var allocator in _allocators) {
                ((ID3D12CommandAllocator*)allocator)->Release();
            }

            _allocators.Clear();
            _retiredAllocators.Clear();
        }
    }
    
    private readonly struct RetiredAllocator(ID3D12CommandAllocator* allocator, ulong completeFenceValue) {
        public readonly ID3D12CommandAllocator* Allocator = allocator;
        public readonly ulong CompleteFenceValue = completeFenceValue;
    }
}