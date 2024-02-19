namespace RiptideMathematics;

public partial struct Color(float red, float green, float blue, float alpha = 1) : IEquatable<Color>, IFormattable {
    public float R = red;
    public float G = green;
    public float B = blue;
    public float A = alpha;

    public Color(float grayscale, float alpha = 1f) : this(grayscale, grayscale, grayscale, alpha) { }

    public readonly Vector4 AsVector4() => Unsafe.BitCast<Color, Vector4>(this);
    public readonly Vector128<float> AsVector128() => Unsafe.BitCast<Color, Vector128<float>>(this);

    public override readonly bool Equals(object? other) => other is Color color && Equals(color);

    public readonly bool Equals(Color other) => Unsafe.BitCast<Color, Vector4>(this) == Unsafe.BitCast<Color, Vector4>(other);

    public override readonly int GetHashCode() => HashCode.Combine(R, G, B, A);

    public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("G", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{R.ToString(format, provider)}{separator} {G.ToString(format, provider)}{separator} {B.ToString(format, provider)}{separator} {A.ToString(format, provider)}>";
    }
    
    public static explicit operator Color(Color32 color) {
        return new Color(color.R, color.G, color.B, color.A) / 255f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static Color operator +(Color left, Color right) => Add(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static Color operator -(Color left, Color right) => Subtract(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static Color operator -(Color left) => Negate(left);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static Color operator *(Color left, Color right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static Color operator *(Color left, float right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static Color operator *(float left, Color right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static Color operator /(Color left, Color right) => Divide(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static Color operator /(Color left, float right) => Divide(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(Color left, Color right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(Color left, Color right) => !(left == right);
}