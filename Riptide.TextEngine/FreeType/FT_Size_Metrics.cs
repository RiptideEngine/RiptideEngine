namespace Riptide.LowLevel.TextEngine.FreeType; 

public readonly struct FT_Size_Metrics {
    public readonly ushort Xppem;
    public readonly ushort Yppem;

    public readonly FT_Fixed XScale;
    public readonly FT_Fixed YScale;

    public readonly FT_Pos Ascender;
    public readonly FT_Pos Descender;
    public readonly FT_Pos Height;
    public readonly FT_Pos MaxAdvance;
}