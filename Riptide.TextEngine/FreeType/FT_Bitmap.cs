namespace Riptide.LowLevel.TextEngine.FreeType;

public unsafe struct FT_Bitmap {
    public uint Rows;
    public uint Width;
    public int Pitch;
    public byte* Buffer;
    public ushort NumGrays;
    public FT_Pixel_Mode PixelMode;
    public byte PaletteMode;
    public void* Palette;
}