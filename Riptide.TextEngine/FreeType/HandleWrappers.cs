namespace Riptide.LowLevel.TextEngine.FreeType;

public readonly record struct FT_Library(nint Handle);

public readonly unsafe struct FT_Face {
    public readonly FT_FaceRec* Handle;
}

public readonly unsafe struct FT_GlyphSlot {
    public readonly FT_GlyphSlotRec* Handle;
}

public readonly unsafe struct FT_CharMap {
    public readonly FT_CharMapRec* Handle;
}

public readonly unsafe struct FT_Size {
    public readonly FT_SizeRec* Handle;
}
public readonly record struct FT_Driver(nint Handle);
public readonly record struct FT_Memory(nint Handle);
public readonly record struct FT_Stream(nint Handle);
public readonly record struct FT_Face_Internal(nint Handle);
public readonly record struct FT_ListNode(nint Handle);
public readonly record struct FT_SubGlyph(nint Handle);
public readonly record struct FT_Slot_Internal(nint Handle);