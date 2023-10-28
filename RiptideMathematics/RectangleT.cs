namespace RiptideMathematics;

[JsonConverter(typeof(RectangleConverterFactory))]
public partial struct Rectangle<T>(T x, T y, T w, T h) : IEquatable<Rectangle<T>>, IFormattable where T : unmanaged, INumber<T> {
    [JsonInclude] public T X = x, Y = y, W = w, H = h;

    [JsonIgnore] public readonly T Area => W * H;
    [JsonIgnore] public readonly T Perimeter => W + W + H + H;

    public override readonly int GetHashCode() => HashCode.Combine(X, Y, W, H);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Rectangle<T> other && Equals(other);
    public readonly bool Equals(Rectangle<T> other) => X == other.X && Y == other.Y && W == other.W && H == other.H;

    public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("G", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{X.ToString(format, provider)}{separator} {Y.ToString(format, provider)}{separator} {W.ToString(format, provider)}{separator} {H.ToString(format, provider)}>";
    }
}