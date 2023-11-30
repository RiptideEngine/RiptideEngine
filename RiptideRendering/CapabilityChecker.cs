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
public abstract class CapabilityChecker {
    /// <summary>
    /// Determine whether the rendering API supports pixel shader specified stencil reference for finer control of stencil value.
    /// </summary>
    public abstract bool SupportShaderSpecifiedStencilRef { get; }
    
    public abstract TextureSupportFlags CheckTextureFormatSupport(GraphicsFormat format);
    public abstract (uint Dimension, uint Array) GetMaximumTextureSize(TextureDimension dimension);
}