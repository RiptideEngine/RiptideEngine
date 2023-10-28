namespace RiptideRendering;

[Flags]
public enum TextureSupportFlags {
    None = 0,

    Texture1D = 1 << 0,
    Texture2D = 1 << 1,
    Texture3D = 1 << 2,
    TextureCube = 1 << 3,
    RenderTarget = 1 << 4,
    DepthTexture = 1 << 5,
}

/// <summary>
/// Base class for rendering context's device capacity/feature checking.
/// </summary>
public abstract class BaseCapabilityChecker {
    /// <summary>
    /// Highest shader model support on the hardware.
    /// </summary>
    public abstract ShaderModel HighestShaderModel { get; }

    /// <summary>
    /// Determine whether the rendering API use Direct3D12's root signature feature. Return <see langword="true"/> on Direct3D12 only.
    /// </summary>
    public abstract bool UseRootSignature { get; }

    /// <summary>
    /// Maximum dimension of every Texture2D. <br/>
    /// Note: Hardcoded to 4096 for the time being.
    /// </summary>
    public const ushort MaximumTexture2DDimension = 4096;

    /// <summary>
    /// Determine whether the rendering API supports push constants in Vulkan or Root Constants in Direct3D12.
    /// </summary>
    public abstract bool SupportRootConstants { get; }

    /// <summary>
    /// Determine whether the rendering API supports pixel shader specified stencil reference for finer control of stencil value.
    /// </summary>
    public abstract bool SupportShaderSpecifiedStencilRef { get; }

    public abstract TextureSupportFlags CheckTextureFormatSupport(GraphicsFormat format);
}