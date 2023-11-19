using System.Runtime.InteropServices;

namespace Riptide.LowLevel.TextEngine.FreeType;

#pragma warning disable CA1401

public static unsafe partial class FreeTypeBinding {
    [LibraryImport("freetype")]
    public static partial FT_Error FT_Init_FreeType(out FT_Library output);

    [LibraryImport("freetype")]
    public static partial FT_Error FT_Done_FreeType(FT_Library library);

    [LibraryImport("freetype")]
    public static partial FT_Error FT_Property_Set(FT_Library library, [MarshalAs(UnmanagedType.LPStr)] string moduleName, [MarshalAs(UnmanagedType.LPStr)] string propertyName, void* value);
    
    [LibraryImport("freetype")]
    public static partial FT_Error FT_New_Face(FT_Library library, [MarshalAs(UnmanagedType.LPStr)] string filePath, int faceIndex, out FT_Face output);

    [LibraryImport("freetype")]
    public static partial FT_Error FT_New_Memory_Face(FT_Library library, byte* fileBase, int fileSize, int faceIndex, out FT_Face pOutputFace);

    [LibraryImport("freetype")]
    public static partial FT_Error FT_Done_Face(FT_Face face);

    [LibraryImport("freetype")]
    public static partial FT_Error FT_Set_Charmap(FT_Face face, FT_CharMap charmap);
    
    [LibraryImport("freetype")]
    public static partial FT_Error FT_Select_Charmap(FT_Face face, FT_Encoding encoding);
    
    [LibraryImport("freetype")]
    public static partial FT_Error FT_Set_Char_Size(FT_Face face, FT_F26Dot6 charWidth, FT_F26Dot6 charHeight, uint horizontalResolution, uint verticalResolution);

    [LibraryImport("freetype")]
    public static partial FT_Error FT_Set_Pixel_Sizes(FT_Face face, uint pixelWidth, uint pixelHeight);

    [LibraryImport("freetype")]
    public static partial uint FT_Get_Char_Index(FT_Face face, uint charcode);

    [LibraryImport("freetype")]
    public static partial FT_Error FT_Load_Glyph(FT_Face face, uint glyphIndex, FT_Load_Flags loadFlags);

    [LibraryImport("freetype")]
    public static partial FT_Error FT_Render_Glyph(FT_GlyphSlot slot, FT_Render_Mode mode);
    
    [LibraryImport("freetype")]
    public static partial byte* FT_Error_String(FT_Error error);
}

#pragma warning restore CA1401