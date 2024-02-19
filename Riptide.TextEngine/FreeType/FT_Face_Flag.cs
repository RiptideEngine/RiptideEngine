namespace Riptide.LowLevel.TextEngine.FreeType;

public enum FT_Face_Flag {
    Scalable = 1 <<  0,
    FixedSizes = 1 <<  1,
    FixedWidth = 1 <<  2,
    SFNT = 1 <<  3,
    Horizontal = 1 <<  4,
    Vertical = 1 <<  5,
    Kerning = 1 <<  6,
    FastGlyphs = 1 <<  7,
    MultipleMasters = 1 <<  8,
    GlyphNames = 1 <<  9,
    ExternalStream  = 1 << 10,
    Hinter = 1 << 11,
    CIDKeyed = 1 << 12,
    Tricky = 1 << 13,
    Color = 1 << 14,
    Variation = 1 << 15,
    SVG = 1 << 16,
    SBIX = 1 << 17,
    SBIXOverlay = 1 << 18,
}