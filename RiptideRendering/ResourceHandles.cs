namespace RiptideRendering;

public readonly record struct NativeBufferHandle(ulong Handle) {
    public static implicit operator ulong(NativeBufferHandle handle) => handle.Handle;
}

public readonly record struct NativeTextureHandle(ulong Handle) {
    public static implicit operator ulong(NativeTextureHandle handle) => handle.Handle;
}

public readonly record struct NativeReadbackBufferHandle(ulong Handle) {
    public static implicit operator ulong(NativeReadbackBufferHandle handle) => handle.Handle;
}