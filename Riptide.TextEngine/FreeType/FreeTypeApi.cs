namespace Riptide.LowLevel.TextEngine.FreeType;

#pragma warning disable CA1401

public static unsafe partial class FreeTypeApi {
    private const string LibraryName = "freetype.dll";
    
    [LibraryImport(LibraryName)]
    public static partial FT_Error FT_Init_FreeType(out FT_Library output);

    [LibraryImport(LibraryName)]
    public static partial FT_Error FT_Done_FreeType(FT_Library library);

    [LibraryImport(LibraryName)]
    public static partial FT_Error FT_Property_Set(FT_Library library, [MarshalAs(UnmanagedType.LPStr)] string moduleName, [MarshalAs(UnmanagedType.LPStr)] string propertyName, void* value);
    
    [LibraryImport(LibraryName)]
    public static partial FT_Error FT_New_Face(FT_Library library, [MarshalAs(UnmanagedType.LPStr)] string filePath, int faceIndex, out FT_FaceRec* output);

    [LibraryImport(LibraryName)]
    public static partial FT_Error FT_New_Memory_Face(FT_Library library, byte* fileBase, int fileSize, int faceIndex, out FT_FaceRec* pOutputFace);

    [LibraryImport(LibraryName)]
    public static partial FT_Error FT_Done_Face(FT_FaceRec* face);

    [LibraryImport(LibraryName)]
    public static partial FT_Error FT_Set_Charmap(FT_FaceRec* face, FT_CharMapRec* charmap);
    
    [LibraryImport(LibraryName)]
    public static partial FT_Error FT_Select_Charmap(FT_FaceRec* face, FT_Encoding encoding);
    
    [LibraryImport(LibraryName)]
    public static partial FT_Error FT_Set_Char_Size(FT_FaceRec* face, FT_F26Dot6 charWidth, FT_F26Dot6 charHeight, uint horizontalResolution, uint verticalResolution);

    [LibraryImport(LibraryName)]
    public static partial FT_Error FT_Set_Pixel_Sizes(FT_FaceRec* face, uint pixelWidth, uint pixelHeight);

    [LibraryImport(LibraryName)]
    public static partial uint FT_Get_Char_Index(FT_FaceRec* face, uint charcode);

    [LibraryImport(LibraryName)]
    public static partial FT_Error FT_Load_Glyph(FT_FaceRec* face, uint glyphIndex, FT_Load_Flags loadFlags);

    [LibraryImport(LibraryName)]
    public static partial FT_Error FT_Render_Glyph(FT_GlyphSlotRec* slot, FT_Render_Mode mode);

    [LibraryImport(LibraryName)]
    public static partial FT_Error FT_Get_Kerning(FT_FaceRec* face, uint left_glyph, uint right_glyph, FT_Kerning_Mode kern_mode, out FT_Vector akerning);
    
    [LibraryImport(LibraryName)]
    public static partial byte* FT_Error_String(FT_Error error);
}

#pragma warning restore CA1401