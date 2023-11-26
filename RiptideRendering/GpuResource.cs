namespace RiptideRendering;

public abstract class GpuResource : RiptideRcObject {
    public nint NativeResourceHandle { get; protected set; }
}