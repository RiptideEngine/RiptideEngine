namespace RiptideRendering.Direct3D12;

internal sealed unsafe class CommandQueue : IDisposable {
    private const int TypeBitshift = 58;

    private ComPtr<ID3D12CommandQueue> pQueue;
    private ComPtr<ID3D12Fence> pFence;
    private nint hEvent;
    private CommandAllocatorPool _allocatorPool;

    private ulong _lastCompletedFenceValue, _nextFenceValue;

    public CommandListType Type { get; private set; }
    public ID3D12CommandQueue* Queue => pQueue;
    public ulong NextFenceValue => _nextFenceValue;
    public ulong CompletedValue => pFence.GetCompletedValue();

    public CommandQueue(ID3D12Device* pDevice, CommandListType type) {
        try {
            int hr = pDevice->CreateCommandQueue(new CommandQueueDesc() {
                Type = type,
                NodeMask = 1,
            }, SilkMarshal.GuidPtrOf<ID3D12CommandQueue>(), (void**)pQueue.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            hr = pDevice->CreateFence(0, FenceFlags.None, SilkMarshal.GuidPtrOf<ID3D12Fence>(), (void**)pFence.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            hEvent = SilkMarshal.CreateWindowsEvent(null, false, false, null);
            if (hEvent == nint.Zero) {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            _allocatorPool = new(pDevice, type);

            hr = pFence.Signal((ulong)type << TypeBitshift);
            Marshal.ThrowExceptionForHR(hr);

            _lastCompletedFenceValue = (ulong)type << TypeBitshift;
            _nextFenceValue = _lastCompletedFenceValue + 1;

            Type = type;
        } catch {
            Dispose(true);
            throw;
        }
    }

    public ulong ExecuteCommandList(ID3D12CommandList* pList) {
        pQueue.ExecuteCommandLists(1, &pList);

        Marshal.ThrowExceptionForHR(pQueue.Signal(pFence, _nextFenceValue));
        return _nextFenceValue++;
    }

    public ulong ExecuteCommandLists(uint numLists, ID3D12CommandList** ppLists) {
        pQueue.ExecuteCommandLists(numLists, ppLists);

        Marshal.ThrowExceptionForHR(pQueue.Signal(pFence, _nextFenceValue));
        return _nextFenceValue++;
    }

    public ulong IncrementFence() {
        pQueue.Signal(pFence, _nextFenceValue);
        return _nextFenceValue++;
    }

    public bool IsFenceCompleted(ulong fenceValue) {
        if (fenceValue > _lastCompletedFenceValue) {
            _lastCompletedFenceValue = ulong.Max(_lastCompletedFenceValue, pFence.GetCompletedValue());
        }

        return fenceValue <= _lastCompletedFenceValue;
    }

    public void StallForQueue(CommandQueue queue) {
        pQueue.Wait(queue.pFence, queue._nextFenceValue - 1);
    }

    public void WaitForIdle() { WaitForFence(IncrementFence()); }

    public void WaitForFence(ulong fenceValue) {
        if (IsFenceCompleted(fenceValue)) return;

        pFence.SetEventOnCompletion(fenceValue, (void*)hEvent);
        SilkMarshal.WaitWindowsObjects(hEvent);
        _lastCompletedFenceValue = fenceValue;
    }

    public ID3D12CommandAllocator* RequestAllocator() {
        return _allocatorPool.Request(pFence.GetCompletedValue());
    }

    public void ReturnAllocator(ulong fenceValue, ID3D12CommandAllocator* allocator) {
        _allocatorPool.ReturnAllocator(fenceValue, allocator);
    }

    private void Dispose(bool disposing) {
        if (pQueue.Handle == null) return;

        if (disposing) {
            _allocatorPool.Dispose(); _allocatorPool = null!;
        }

        var _ = SilkMarshal.CloseWindowsHandle(hEvent); hEvent = nint.Zero;
        pQueue.Dispose(); pQueue = default;
        pFence.Dispose(); pFence = default;
    }

    ~CommandQueue() {
        Dispose(disposing: false);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}