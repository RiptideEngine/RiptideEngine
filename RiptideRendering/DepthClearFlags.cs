namespace RiptideRendering;

[Flags]
public enum DepthClearFlags {
    None = 0,

    Depth = 1 << 0,
    Stencil = 1 << 1,

    All = Depth | Stencil,
}