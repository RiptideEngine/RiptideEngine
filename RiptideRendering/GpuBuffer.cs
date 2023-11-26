namespace RiptideRendering;

[Flags]
public enum BufferFlags {
    None = 0,
}

public struct BufferDescription {
    public ulong Width;
    public BufferFlags Flags;
}

public abstract class GpuBuffer : GpuResource {
    public abstract BufferDescription Description { get; }
}