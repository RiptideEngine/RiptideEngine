namespace RiptideMathematics;

public partial struct Circle(Vector2 position, float radius) : IEquatable<Circle>, IFormattable {
    public Vector2 Position = position;
    public float Radius = radius;

    public Circle(float x, float y, float r) : this(new(x, y), r) { }

    public override readonly int GetHashCode() => HashCode.Combine(Position, Radius);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Circle other && Equals(other);
    public readonly bool Equals(Circle other) => Unsafe.BitCast<Circle, Vector3>(this) == Unsafe.BitCast<Circle, Vector3>(other);

    public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("G", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{Position.ToString(format, provider)}{separator} {Radius.ToString(format, provider)}>";
    }

    public static bool operator ==(Circle left, Circle right) => left.Equals(right);
    public static bool operator !=(Circle left, Circle right) => !left.Equals(right);
}