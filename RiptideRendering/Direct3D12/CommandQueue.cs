using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class CommandQueue : IDisposable {
    private ComPtr<ID3D12CommandQueue> pQueue;

    private ComPtr<ID3D12Fence> pFence;
    private nint _eventHandle;
    private ulong _lastCompletedFenceValue;

    private readonly CommandAllocatorPool _allocatorPool;
    private readonly object _fenceLock, _eventLock;

    public ID3D12CommandQueue* Queue => pQueue;

    public ulong LastCompletedFenceValue => _lastCompletedFenceValue;
    public ulong NextFenceValue { get; private set; }

    public CommandQueue(D3D12RenderingContext context, CommandListType type) {
        pQueue = context.Device->CreateCommandQueue<ID3D12CommandQueue>(new CommandQueueDesc {
            Type = type,
        });
        pFence = context.Device->CreateFence<ID3D12Fence>(0, FenceFlags.None);
        pFence.Signal((ulong)type << 58);

        _lastCompletedFenceValue = (ulong)type << 58;
        NextFenceValue = _lastCompletedFenceValue + 1;
        
        _eventHandle = SilkMarshal.CreateWindowsEvent(null, false, false, null);
        Debug.Assert(_eventHandle != nint.Zero);

        _allocatorPool = new(context, type);

        _fenceLock = new();
        _eventLock = new();
    }

    public ulong ExecuteCommandList(ID3D12CommandList* list) {
        lock (_fenceLock) {
            pQueue.ExecuteCommandLists(1, &list);
            pQueue.Signal(pFence, NextFenceValue);

            return NextFenceValue++;
        }
    }

    public bool IsFenceComplete(ulong fenceValue) {
        if (fenceValue > _lastCompletedFenceValue) {
            _lastCompletedFenceValue = ulong.Max(_lastCompletedFenceValue, pFence.GetCompletedValue());
        }

        return fenceValue <= _lastCompletedFenceValue;
    }
    
    public ulong IncrementFence() {
        lock (_fenceLock) {
            pQueue.Signal(pFence, NextFenceValue);
            return NextFenceValue++;
        }
    }
    
    public bool WaitForFence(ulong fenceValue) {
        if (IsFenceComplete(fenceValue))
            return false;

        lock (_eventLock) {
            Console.WriteLine("Waiting fence: " + fenceValue);
            
            pFence.SetEventOnCompletion(fenceValue, (void*)_eventHandle);
            SilkMarshal.WaitWindowsObjects(_eventHandle);
            _lastCompletedFenceValue = fenceValue;

            return true;
        }
    }

    public void WaitForIdle() => WaitForFence(IncrementFence());
    
    public void StallForFence(CommandQueue other) {
        pQueue.Wait(other.pFence, other.NextFenceValue - 1);
    }

    public ID3D12CommandAllocator* RequestAllocator() {
        return _allocatorPool.Request(pFence.GetCompletedValue());
    }

    public void ReturnAllocator(ulong fenceValue, ID3D12CommandAllocator* pAllocator) {
        _allocatorPool.Return(pAllocator, fenceValue);
    }

    private void Dispose(bool disposing) {
        if (pQueue.Handle == null) return;

        _allocatorPool.Dispose();

        SilkMarshal.CloseWindowsHandle(_eventHandle);
        _eventHandle = nint.Zero;

        pFence.Release();
        
        pQueue.Release();
        pQueue = default;
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~CommandQueue() {
        Dispose(false);
    }
}