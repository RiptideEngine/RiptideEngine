namespace RiptideRendering;

public abstract partial class RenderingContext : IDisposable {
    public abstract RenderingAPI RenderingAPI { get; }
    public abstract Factory Factory { get; }
    public abstract CapabilityChecker Capability { get; }
    public abstract Synchronizer Synchronizer { get; }

    public abstract (GpuResource Resource, RenderTargetView View) SwapchainCurrentRenderTarget { get; }

    public void ResizeSwapchain(uint width, uint height) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width, nameof(width));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height, nameof(height));

        ResizeSwapchainImpl(width, height);
    }
    protected abstract void ResizeSwapchainImpl(uint width, uint height);

    public abstract void Present();

    public abstract ulong ExecuteCommandList(CommandList commandList);

    protected abstract void Dispose(bool disposing);

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}