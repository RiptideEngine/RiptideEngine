namespace RiptideRendering;

[EnumExtension]
public enum ResourceDimension {
    Unknown = 0,
    Buffer,
    Texture1D,
    Texture2D,
    Texture3D,
}

[EnumExtension, Flags]
public enum ResourceFlags {
    None = 0,

    RenderTarget = 1 << 0,
    DepthStencil = 1 << 1,
    UnorderedAccess = 1 << 2,
}

public struct ResourceDescriptor {
    public ResourceDimension Dimension;
    public ulong Width;
    public ushort Height;
    public ushort DepthOrArraySize;

    public ResourceFlags Flags;
    public GraphicsFormat TextureFormat;
}

public readonly record struct NativeResourceHandle(nint Handle);

public abstract class GpuResource : RenderingObject {
    public abstract ResourceDescriptor Descriptor { get; }
    public NativeResourceHandle NativeResource { get; protected set; }

    public static implicit operator NativeResourceHandle(GpuResource resource) => resource.NativeResource;
}