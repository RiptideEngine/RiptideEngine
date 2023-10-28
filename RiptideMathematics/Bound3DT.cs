namespace RiptideMathematics;

[JsonConverter(typeof(Bound3DConverterFactory))]
public partial struct Bound3D<T>(T minX, T minY, T minZ, T maxX, T maxY, T maxZ) : IEquatable<Bound3D<T>>, IFormattable where T : unmanaged, INumber<T> {
    [JsonInclude] public T MinX = minX, MinY = minY, MinZ = minZ;
    [JsonInclude] public T MaxX = maxX, MaxY = maxY, MaxZ = maxZ;

    [JsonIgnore] public readonly T Area => (MaxX - MinX) * (MaxY - MinY) * (MaxZ - MinZ);
    [JsonIgnore] public readonly T Perimeter => T.CreateChecked(2) * (MaxX - MinX + MaxY - MinY + MaxZ - MinZ);

    public override readonly int GetHashCode() => HashCode.Combine(MinX, MinY, MaxX, MaxY, MinZ, MaxZ);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Bound3D<T> other && Equals(other);
    public readonly bool Equals(Bound3D<T> other) => MinX == other.MinX && MinY == other.MinY && MaxX == other.MaxX && MaxY == other.MaxY && MinZ == other.MinZ && MaxZ == other.MaxZ;

    public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("G", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{MinX.ToString(format, provider)}{separator} {MinY.ToString(format, provider)}{separator} {MaxX.ToString(format, provider)}{separator} {MaxY.ToString(format, provider)}{separator} {MinZ.ToString(format, provider)}{separator} {MaxZ.ToString(format, provider)}>";
    }
}