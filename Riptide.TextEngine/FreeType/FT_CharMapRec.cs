namespace Riptide.LowLevel.TextEngine.FreeType; 

public struct FT_CharMapRec {
    public FT_Face Face;
    public FT_Encoding Encoding;
    public ushort PlatformID;
    public ushort EncodingID;
}