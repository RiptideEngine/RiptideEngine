using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal sealed class CommandQueues(D3D12RenderingContext context) : IDisposable {
    private bool _disposed;

    public CommandQueue GraphicQueue { get; } = new(context, CommandListType.Direct);
    public CommandQueue ComputeQueue { get; } = new(context, CommandListType.Compute);
    public CommandQueue CopyQueue { get; } = new(context, CommandListType.Copy);

    public CommandQueue GetQueue(ulong fenceValue) {
        return (fenceValue >> 58) switch {
            (uint)CommandListType.Compute => ComputeQueue,
            (uint)CommandListType.Copy => CopyQueue,
            _ => GraphicQueue,
        };
    }

    public void IdleGpu() {
        GraphicQueue.WaitForIdle();
        ComputeQueue.WaitForIdle();
        CopyQueue.WaitForIdle();
    }

    private void Dispose(bool disposing) {
        if (_disposed) return;
        
        IdleGpu();

        GraphicQueue.Dispose();
        ComputeQueue.Dispose();
        ComputeQueue.Dispose();
        
        _disposed = true;
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~CommandQueues() {
        Dispose(false);
    }
}