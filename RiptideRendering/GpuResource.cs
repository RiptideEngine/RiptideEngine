namespace RiptideRendering;

public abstract unsafe class GpuResource : RenderingObject {
    public nint NativeResourceHandle { get; protected set; }
}