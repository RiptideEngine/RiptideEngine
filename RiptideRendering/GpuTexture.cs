namespace RiptideRendering;

[EnumExtension]
public enum TextureDimension {
    Unknown = 0,
    
    Texture1D = 1,
    Texture2D = 2,
    Texture3D = 3,
}

[Flags]
public enum TextureFlags {
    None = 0,
    
    RenderTarget = 1 << 0,
    DepthStencil = 1 << 1,
}

public struct TextureDescription {
    public TextureDimension Dimension;
    public uint Width;
    public ushort Height;
    public ushort DepthOrArraySize;
    public uint MipLevels;
    public GraphicsFormat Format;
    public TextureFlags Flags;
}

public abstract class GpuTexture : GpuResource {
    public abstract TextureDescription Description { get; }
}