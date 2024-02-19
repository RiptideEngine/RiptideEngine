namespace Riptide.LowLevel.TextEngine.FreeType; 

public readonly unsafe struct FT_SizeRec {
    public readonly FT_FaceRec* Face;
    public readonly FT_Generic Generic;
    public readonly FT_Size_Metrics Metrics;
    private readonly nint _internal;
}