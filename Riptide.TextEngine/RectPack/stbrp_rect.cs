using System.Runtime.InteropServices;

namespace Riptide.LowLevel.TextEngine.RectPack; 

public struct stbrp_rect {
    public int ID;
    
    // Inputs
    public int Width, Height;

    // Outputs:
    public int X, Y;
    [MarshalAs(UnmanagedType.Bool)] public bool WasPacked;  // non-zero if valid packing
}