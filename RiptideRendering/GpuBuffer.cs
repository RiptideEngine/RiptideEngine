namespace RiptideRendering;

[EnumExtension]
public enum BufferFlags {
    None = 0,

    // UnorderedAccess = 1 << 0,
    // Counter = 1 << 0,
}

public struct BufferDescriptor {
    public ulong Size;
    public BufferFlags Flags;
}

public abstract class GpuBuffer : GpuResource {
    public NativeBufferHandle ResourceHandle { get; protected set; }
    public abstract BufferDescriptor Descriptor { get; }

    public static implicit operator NativeBufferHandle(GpuBuffer buffer) => buffer.ResourceHandle;
}