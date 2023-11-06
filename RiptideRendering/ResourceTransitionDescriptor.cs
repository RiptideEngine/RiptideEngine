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

public readonly record struct ResourceTransitionDescriptor(NativeResourceHandle Target, ResourceStates OldStates, ResourceStates NewStates);