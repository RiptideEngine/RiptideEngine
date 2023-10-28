namespace RiptideRendering;

[Flags]
public enum ResourceStates {
    Common = 0,

    IndexBuffer = 1 << 0,
    ConstantBuffer = 1 << 1,
    ShaderResource = 1 << 2,
    UnorderedAccess = 1 << 3,
    RenderTarget = 1 << 4,
    CopySource = 1 << 5,
    CopyDestination = 1 << 6,
    DepthWrite = 1 << 7,
    DepthRead = 1 << 8,
    Present = 1 << 9,
}

public readonly struct ResourceTransitionTarget {
    public readonly ulong ResourceHandle;

    public ResourceTransitionTarget(NativeBufferHandle buffer) {
        ResourceHandle = buffer.Handle;
    }

    public ResourceTransitionTarget(NativeTextureHandle texture) {
        ResourceHandle = texture.Handle;
    }

    public static implicit operator ResourceTransitionTarget(NativeBufferHandle buffer) => new(buffer);
    public static implicit operator ResourceTransitionTarget(NativeTextureHandle texture) => new(texture);

    public static implicit operator ResourceTransitionTarget(GpuBuffer buffer) => new(buffer);
    public static implicit operator ResourceTransitionTarget(GpuTexture texture) => new(texture);
}

public readonly struct ResourceTransitionDescriptor(ResourceTransitionTarget target, ResourceStates oldStates, ResourceStates newStates) {
    public readonly ResourceTransitionTarget Target = target;

    public readonly ResourceStates OldStates = oldStates;
    public readonly ResourceStates NewStates = newStates;
}