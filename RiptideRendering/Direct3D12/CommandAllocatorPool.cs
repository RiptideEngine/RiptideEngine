namespace RiptideRendering.Direct3D12;

internal sealed unsafe class CommandAllocatorPool : IDisposable {
    private struct ReadyAllocator {
        public ulong FenceValue;
        public ID3D12CommandAllocator* Allocator;

        public ReadyAllocator(ulong fenceValue, ID3D12CommandAllocator* pAllocator) {
            FenceValue = fenceValue;
            Allocator = pAllocator;
        }
    }

    private readonly List<nint> _allocators;
    private readonly Queue<ReadyAllocator> _readyAllocators;

    private ID3D12Device* pDevice;
    private readonly object _lock;
    private readonly CommandListType _type;

    public CommandAllocatorPool(ID3D12Device* pDevice, CommandListType type) {
        this.pDevice = pDevice;
        pDevice->AddRef();

        _allocators = new();
        _readyAllocators = new();

        _lock = new();
        _type = type;
    }

    public ID3D12CommandAllocator* Request(ulong completedFenceValue) {
        lock (_lock) {
            if (_readyAllocators.TryPeek(out var peeked)) {
                if (peeked.FenceValue <= completedFenceValue) {
                    var allocator = _readyAllocators.Dequeue().Allocator;
                    Debug.Assert(allocator->Reset() >= 0, "CommandAllocatorPool.Request: Failed to reset ID3D12CommandAllocator.");

                    return allocator;
                }
            }

            ID3D12CommandAllocator* pOutput = default;
            int hr = pDevice->CreateCommandAllocator(_type, SilkMarshal.GuidPtrOf<ID3D12CommandAllocator>(), (void**)&pOutput);
            Marshal.ThrowExceptionForHR(hr);

            _allocators.Add((nint)pOutput);

            Console.WriteLine($"Direct3D12 - CommandAllocatorPool: New allocator 0x{(nint)pOutput:X8} created at fence value {completedFenceValue}.");

            return pOutput;
        }
    }

    public void ReturnAllocator(ulong fenceValue, ID3D12CommandAllocator* allocator) {
        lock (_lock) {
            _readyAllocators.Enqueue(new(fenceValue, allocator));
        }
    }

    private void Dispose(bool disposing) {
        if (pDevice == null) return;

        foreach (var ptr in _allocators) {
            ((ID3D12CommandAllocator*)ptr)->Release();
        }
        _allocators.Clear();
        _readyAllocators.Clear();

        pDevice->Release(); pDevice = null;
    }

    ~CommandAllocatorPool() {
        Dispose(disposing: false);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}