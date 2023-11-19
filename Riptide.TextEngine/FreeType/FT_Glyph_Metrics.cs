namespace Riptide.LowLevel.TextEngine.FreeType;

public readonly struct FT_Glyph_Metrics {
    public readonly FT_Pos Width;
    public readonly FT_Pos Height;

    public readonly FT_Pos HoriBearingX;
    public readonly FT_Pos HoriBearingY;
    public readonly FT_Pos HoriAdvance;

    public readonly FT_Pos VertBearingX;
    public readonly FT_Pos VertBearingY;
    public readonly FT_Pos VertAdvance;
}