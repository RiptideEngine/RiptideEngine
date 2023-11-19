namespace Riptide.LowLevel.TextEngine.FreeType;

public unsafe struct FT_Outline {
    public short NumContours;
    public short NumPoints;

    public FT_Vector* Points;
    public byte* Tags;
    public short* Contours;

    public int Flags;
}