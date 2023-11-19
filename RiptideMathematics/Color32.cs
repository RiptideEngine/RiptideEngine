namespace RiptideMathematics; 

public partial struct Color32(byte red, byte green, byte blue, byte alpha = 255) : IEquatable<Color32>, IFormattable {
    public byte R = red;
    public byte G = green;
    public byte B = blue;
    public byte A = alpha;

    public Color32(byte grayscale, byte alpha) : this(grayscale, grayscale, grayscale, alpha) { }

    public override readonly bool Equals(object? other) => other is Color32 color && Equals(color);

    public readonly bool Equals(Color32 other) => Unsafe.BitCast<Color32, uint>(this) == Unsafe.BitCast<Color32, uint>(other);

    public override readonly int GetHashCode() => (int)Unsafe.BitCast<Color32, uint>(this);

    public override readonly string ToString() => ToString("D", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("D", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{R.ToString(format, provider)}{separator} {G.ToString(format, provider)}{separator} {B.ToString(format, provider)}{separator} {A.ToString(format, provider)}>";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static Color32 operator +(Color32 left, Color32 right) => Add(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static Color32 operator -(Color32 left, Color32 right) => Subtract(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static Color32 operator *(Color32 left, Color32 right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static Color32 operator /(Color32 left, Color32 right) => Divide(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(Color32 left, Color32 right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(Color32 left, Color32 right) => !(left == right);
}