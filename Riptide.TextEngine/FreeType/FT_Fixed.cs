namespace Riptide.LowLevel.TextEngine.FreeType;

public readonly record struct FT_Fixed(int Value) {
    public static explicit operator FT_Fixed(float value) => new((int)(value * (1 << 16)));
}