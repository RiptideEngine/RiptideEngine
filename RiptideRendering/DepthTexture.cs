namespace RiptideRendering;

public readonly record struct DepthStencilHandle(ulong Handle) {
    public static implicit operator ulong(DepthStencilHandle handle) => handle.Handle;
}

public struct DepthTextureDescriptor {
    public uint Width;
    public uint Height;
    public GraphicsFormat Format;
    public ResourceStates InitialStates;
}

public abstract class DepthTexture : GpuTexture {
    public DepthStencilHandle DepthStencilHandle { get; protected set; }
    public abstract DepthTextureDescriptor Descriptor { get; }

    public static implicit operator DepthStencilHandle(DepthTexture dt) => dt.DepthStencilHandle;
}