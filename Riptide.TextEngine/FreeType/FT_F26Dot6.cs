namespace Riptide.LowLevel.TextEngine.FreeType;

public readonly record struct FT_F26Dot6(int Value) {
    public static explicit operator FT_F26Dot6(float value) => new((int)(value * (1 << 6)));
    public static explicit operator FT_F26Dot6(double value) => new((int)(value * (1 << 6)));
    public static implicit operator float(FT_F26Dot6 value) => (float)value.Value / (1 << 6);
    public static implicit operator double(FT_F26Dot6 value) => (double)value.Value / (1 << 6);
}