namespace Riptide.LowLevel.TextEngine.FreeType;

public readonly record struct FT_Library(nint Handle);

public readonly record struct FT_Driver(nint Handle);
public readonly record struct FT_Memory(nint Handle);
public readonly record struct FT_Stream(nint Handle);
public readonly record struct FT_Face_Internal(nint Handle);
public readonly record struct FT_ListNode(nint Handle);
public readonly record struct FT_SubGlyph(nint Handle);
public readonly record struct FT_Slot_Internal(nint Handle);