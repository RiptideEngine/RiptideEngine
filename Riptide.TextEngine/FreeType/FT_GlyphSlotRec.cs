namespace Riptide.LowLevel.TextEngine.FreeType;

public unsafe struct FT_GlyphSlotRec {
    public readonly FT_Library Library;
    public readonly FT_FaceRec* Face;
    public readonly FT_GlyphSlotRec* Next;
    public readonly uint Glyph_index;
    public FT_Generic Generic;

    public readonly FT_Glyph_Metrics Metrics;
    public readonly FT_Fixed LinearHoriAdvance;
    public readonly FT_Fixed LinearVertAdvance;
    public readonly FT_Vector Advance;

    public readonly FT_Glyph_Format Format;

    public readonly FT_Bitmap Bitmap;
    public readonly int BitmapLeft;
    public readonly int BitmapTop;

    public readonly FT_Outline Outline;

    public readonly uint NumSubGlyphs;
    public readonly FT_SubGlyph Subglyphs;

    public readonly void* ControlData;
    public readonly long ControlLen;

    public readonly FT_Pos LsbDelta;
    public readonly FT_Pos RsbDelta;

    public readonly void* Other;

    private readonly FT_Slot_Internal _internal;
}