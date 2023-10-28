namespace RiptideRendering;

public abstract class BaseRenderingContext : IDisposable {
    protected bool disposed;

    public abstract RenderingAPI RenderingAPI { get; }
    public abstract BaseFactory Factory { get; }
    public abstract BaseCapabilityChecker CapabilityChecker { get; }

    public abstract RenderTarget SwapchainCurrentRenderTarget { get; }
    public abstract DepthTexture SwapchainDepthTexture { get; }

    public void ResizeSwapchain(uint width, uint height) {
        ArgumentOutOfRangeException.ThrowIfZero(width, nameof(width));
        ArgumentOutOfRangeException.ThrowIfZero(height, nameof(height));

        ResizeSwapchainImpl(width, height);
    }
    protected abstract void ResizeSwapchainImpl(uint width, uint height);

    public abstract void Present();

    public abstract void WaitForGpuIdle();
    public abstract void ExecuteCommandList(CommandList commandList);
    public abstract void ExecuteCommandLists(ReadOnlySpan<CommandList> commandLists);

    protected abstract void Dispose(bool disposing);

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}