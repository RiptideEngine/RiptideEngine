namespace RiptideMathematics;

public partial struct Sphere(Vector3 position, float radius) : IEquatable<Sphere>, IFormattable {
    public Vector3 Position = position;
    public float Radius = radius;

    public Sphere(float x, float y, float z, float r) : this(new(x, y, z), r) { }

    public override readonly int GetHashCode() => HashCode.Combine(Position, Radius);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Sphere other && Equals(other);
    public readonly bool Equals(Sphere other) => Unsafe.BitCast<Sphere, Vector4>(this) == Unsafe.BitCast<Sphere, Vector4>(other);

    public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("G", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{Position.ToString(format, provider)}{separator} {Radius.ToString(format, provider)}>";
    }

    public static bool operator ==(Sphere left, Sphere right) => left.Equals(right);
    public static bool operator !=(Sphere left, Sphere right) => !left.Equals(right);
}