namespace RiptideRendering;

[Flags]
public enum RenderTargetClearFlags {
    Color = 1 << 0,
    Depth = 1 << 1,
    Stencil = 1 << 2,

    All = Color | Depth | Stencil,
}