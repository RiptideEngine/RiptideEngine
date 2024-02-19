namespace RiptideRendering;

[Flags]
public enum BufferFlags {
    None = 0,
    
    UnorderedAccess = 1 << 0,
}

[EnumExtension]
public enum BufferType {
    Default,
    Dynamic,
}

public struct BufferDescription {
    public BufferType Type;
    public ulong Width;
    public BufferFlags Flags;
}

public abstract class GpuBuffer : GpuResource {
    public abstract BufferDescription Description { get; }
}