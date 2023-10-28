namespace RiptideRendering;

public struct Texture2DDescriptor {
    public uint Width;
    public uint Height;
    public GraphicsFormat Format;

    // public uint ArraySize;
    // public uint MipLevels;
}

public abstract class Texture2D : GpuTexture {
    public abstract Texture2DDescriptor Descriptor { get; }
}