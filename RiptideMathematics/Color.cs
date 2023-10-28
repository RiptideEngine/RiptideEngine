namespace RiptideMathematics;

[JsonConverter(typeof(ColorConverter))]
public unsafe partial struct Color(float red, float green, float blue, float alpha) : IEquatable<Color>, IFormattable {
    [JsonInclude] public float R = red;
    [JsonInclude] public float G = green;
    [JsonInclude] public float B = blue;
    [JsonInclude] public float A = alpha;

    public Color(float grayscale, float alpha) : this(grayscale, grayscale, grayscale, alpha) { }
    public Color(float red, float green, float blue) : this(red, green, blue, 1) { }

    public readonly Vector4 AsVector4() => Unsafe.BitCast<Color, Vector4>(this);
    public readonly Vector128<float> AsVector128() => Unsafe.BitCast<Color, Vector128<float>>(this);

    public override readonly bool Equals(object? other) => other is Color color && Equals(color);

    public readonly bool Equals(Color other) {
        if (Vector128.IsHardwareAccelerated) {
            if (Sse2.IsSupported) {
                return AsVector128() == Sse.LoadVector128((float*)&other);
            }
            if (AdvSimd.IsSupported) {
                return AsVector128() == AdvSimd.LoadVector128((float*)&other);
            }
        }

        return R == other.R && G == other.G && B == other.B && A == other.A;
    }

    public override readonly int GetHashCode() => HashCode.Combine(R, G, B, A);

    public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("G", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{R.ToString(format, provider)}{separator} {G.ToString(format, provider)}{separator} {B.ToString(format, provider)}{separator} {A.ToString(format, provider)}>";
    }
}