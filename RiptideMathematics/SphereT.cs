namespace RiptideMathematics;

public partial struct Sphere<T>(T x, T y, T z, T radius) : IEquatable<Sphere<T>>, IFormattable where T : unmanaged, INumber<T> {
    [JsonInclude] public T X = x, Y = y, Z = z, Radius = radius;

    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z, Radius);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Sphere<T> other && Equals(other);
    public readonly bool Equals(Sphere<T> other) => X == other.X && Y == other.Y && Z == other.Z && Radius == other.Radius;

    public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("G", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{X.ToString(format, provider)}{separator} {Y.ToString(format, provider)}{separator} {Z.ToString(format, provider)}{separator} {Radius.ToString(format, provider)}>";
    }
}