namespace RiptideRendering;

public struct ReadbackBufferDescriptor {
    public uint Size;
}

public abstract class ReadbackBuffer : RenderingObject {
    public abstract ReadbackBufferDescriptor Descriptor { get; }
    public NativeReadbackBufferHandle ResourceHandle { get; protected set; }

    public static implicit operator NativeReadbackBufferHandle(ReadbackBuffer buffer) => buffer.ResourceHandle;

    public abstract ReadOnlySpan<byte> GetMappedData();
}