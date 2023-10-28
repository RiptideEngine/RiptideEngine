namespace RiptideRendering;

public readonly record struct TextureViewHandle(ulong Handle) {
    public static implicit operator ulong(TextureViewHandle handle) => handle.Handle;
}

public abstract class GpuTexture : GpuResource {
    public NativeTextureHandle ResourceHandle { get; protected set; }
    public TextureViewHandle ViewHandle { get; protected set; }

    public static implicit operator NativeTextureHandle(GpuTexture texture) => texture.ResourceHandle;
    public static implicit operator TextureViewHandle(GpuTexture texture) => texture.ViewHandle;
}