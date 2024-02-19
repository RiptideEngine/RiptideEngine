﻿namespace Riptide.LowLevel.TextEngine.FreeType;

public readonly unsafe struct FT_FaceRec {
    public readonly int NumFaces;
    public readonly int FaceIndex;
    
    public readonly FT_Face_Flag FaceFlags;
    public readonly int StyleFlags;
    
    public readonly int NumGlyphs;

    public readonly char* FamilyName;
    public readonly char* StyleName;
    
    public readonly int NumFixedSizes;
    public readonly FT_Bitmap_Size* AvailableSizes;

    public readonly int NumCharmaps;
    public readonly FT_CharMapRec** Charmaps;

    public readonly FT_Generic Generic;

    public readonly FT_BBox Bbox;

    public readonly ushort UnitsPerEm;
    public readonly short Ascender;
    public readonly short Descender;
    public readonly short Height;

    public readonly short MaxAdvanceWidth;
    public readonly short MaxAdvanceHeight;

    public readonly short UnderlinePosition;
    public readonly short UnderlineThickness;

    public readonly FT_GlyphSlotRec* Glyph;
    public readonly FT_SizeRec* Size;
    public readonly FT_CharMapRec* Charmap;

    private readonly FT_Driver _driver;
    private readonly FT_Memory _memory;
    private readonly FT_Stream _stream;

    private readonly FT_ListRec _sizes_list;

    private readonly FT_Generic _autohint;
    private readonly void* _extensions;

    private readonly FT_Face_Internal _internal;
}