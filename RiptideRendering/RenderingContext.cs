namespace RiptideRendering;

public enum QueueType {
    Graphics,
    Compute,
    Copy,
}

public abstract partial class RenderingContext : IDisposable {
    protected bool disposed;

    public abstract RenderingAPI RenderingAPI { get; }
    public abstract Factory Factory { get; }
    public abstract CapabilityChecker CapabilityChecker { get; }

    public abstract (GpuResource Resource, RenderTargetView View) SwapchainCurrentRenderTarget { get; }

    public abstract ILoggingService? Logger { get; set; }

    public void ResizeSwapchain(uint width, uint height) {
        ArgumentOutOfRangeException.ThrowIfZero(width, nameof(width));
        ArgumentOutOfRangeException.ThrowIfZero(height, nameof(height));

        ResizeSwapchainImpl(width, height);
    }
    protected abstract void ResizeSwapchainImpl(uint width, uint height);

    public abstract void Present();

    public abstract void WaitQueueIdle(QueueType type);
    public abstract bool WaitFence(ulong fenceValue);
    public abstract ulong ExecuteCommandList(CopyCommandList commandList);
    public abstract ulong ExecuteCommandList(GraphicsCommandList commandList);

    protected abstract void Dispose(bool disposing);

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}