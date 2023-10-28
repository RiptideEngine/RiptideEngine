namespace RiptideRendering;

public enum DepthTextureClearFlags {
    Depth = 1 << 0,
    Stencil = 1 << 1,

    All = Depth | Stencil,
}