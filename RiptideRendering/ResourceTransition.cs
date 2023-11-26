namespace RiptideRendering;

[Flags]
public enum ResourceTranslateStates {
    Common = 0,
    
    Present = 1 << 0,
    ConstantBuffer = 1 << 1,
    IndexBuffer = 1 << 2,
    RenderTarget = 1 << 3,
    UnorderedAccess = 1 << 4,
    DepthWrite = 1 << 5,
    DepthRead = 1 << 6,
    ShaderResource = 1 << 7,
    // IndirectArgument = 1 << 8,
    CopyDestination = 1 << 9,
    CopySource = 1 << 10,
}

public readonly record struct ResourceTransition(GpuResource Resource, ResourceTranslateStates NewState);