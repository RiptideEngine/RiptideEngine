namespace RiptideMathematics;

[JsonConverter(typeof(Bound2DConverterFactory))]
public partial struct Bound2D<T>(T minX, T minY, T maxX, T maxY) : IEquatable<Bound2D<T>>, IFormattable where T : unmanaged, INumber<T> {
    [JsonInclude] public T MinX = minX, MinY = minY;
    [JsonInclude] public T MaxX = maxX, MaxY = maxY;

    [JsonIgnore] public readonly T Area => (MaxX - MinX) * (MaxY - MinY);
    [JsonIgnore] public readonly T Perimeter => T.CreateChecked(2) * (MaxX - MinX + MaxY - MinY);

    public override readonly int GetHashCode() => HashCode.Combine(MinX, MinY, MaxX, MaxY);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Bound2D<T> other && Equals(other);
    public readonly bool Equals(Bound2D<T> other) => MinX == other.MinX && MinY == other.MinY && MaxX == other.MaxX && MaxY == other.MaxY;

    public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("G", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{MinX.ToString(format, provider)}{separator} {MinY.ToString(format, provider)}{separator} {MaxX.ToString(format, provider)}{separator} {MaxY.ToString(format, provider)}>";
    }
}