namespace RiptideRendering;

public readonly record struct RenderTargetHandle(ulong Handle) {
    public static implicit operator ulong(RenderTargetHandle handle) => handle.Handle;
}

public struct RenderTargetDescriptor {
    public uint Width;
    public uint Height;
    public GraphicsFormat Format;
    public ResourceStates InitialStates;
}

public abstract class RenderTarget : GpuTexture {
    public RenderTargetHandle RenderTargetHandle { get; protected set; }
    public abstract RenderTargetDescriptor Descriptor { get; }

    public static implicit operator RenderTargetHandle(RenderTarget rt) => rt.RenderTargetHandle;
}