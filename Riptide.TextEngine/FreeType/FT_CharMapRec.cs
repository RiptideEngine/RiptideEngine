namespace Riptide.LowLevel.TextEngine.FreeType; 

public unsafe struct FT_CharMapRec {
    public FT_FaceRec* Face;
    public FT_Encoding Encoding;
    public ushort PlatformID;
    public ushort EncodingID;
}