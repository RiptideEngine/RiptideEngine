namespace RiptideRendering.Direct3D12;

internal sealed class CommandQueues(D3D12RenderingContext context) : IDisposable {
    private bool _disposed;

    // TODO: Support multi-queue and figure out how to managing resource on different destruction timeline.
    public CommandQueue GraphicsQueue { get; } = new(context, D3D12CommandListType.Direct);

    public void IdleGpu() {
        GraphicsQueue.WaitForIdle();
    }

    private void Dispose(bool disposing) {
        if (_disposed) return;
        
        IdleGpu();

        GraphicsQueue.Dispose();
        
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