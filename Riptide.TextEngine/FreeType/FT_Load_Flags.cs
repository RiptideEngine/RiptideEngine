namespace Riptide.LowLevel.TextEngine.FreeType;

[Flags]
public enum FT_Load_Flags {
    Default = 0x0,
    NoScale = 1 << 0,
    NoHinting = 1 << 1,
    Render = 1 << 2,
    NoBitmap = 1 << 3,
    VerticalLayout = 1 << 4,
    ForceAutohint = 1 << 5,
    CropBitmap = 1 << 6,
    Pedantic = 1 << 7,
    IgnoreGlobalAdvanceWidth = 1 << 9,
    NoRecurse = 1 << 10,
    IgnoreTransform = 1 << 11,
    Monochrome = 1 << 12,
    LinearDesign = 1 << 13,
    SBitsOnly = 1 << 14,
    NoAutohint = 1 << 15,
    Color = 1 << 20,
    ComputeMetrics = 1 << 21,
    BitmapMetricsOnly = 1 << 22,
    NoSvg = 1 << 24,
}