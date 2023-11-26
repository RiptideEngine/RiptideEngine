namespace RiptideMathematics;

public partial struct Bound2DInt(Vector2Int min, Vector2Int max) : IEquatable<Bound2DInt>, IFormattable {
    public Vector2Int Min = min;
    public Vector2Int Max = max;

    public readonly float Area {
        get {
            var size = Size;
            return size.X * size.Y;
        }
    }
    public readonly float Perimeter {
        get {
            var size = Size;
            return 2 * (size.X + size.Y);
        }
    }

    public readonly Vector2Int Size => Max - Min;

    public Bound2DInt(Vector2Int min, int maxX, int maxY) : this(min, new(maxX, maxY)) { }
    public Bound2DInt(int minX, int minY, Vector2Int max) : this(new(minX, minY), max) { }
    public Bound2DInt(int minX, int minY, int maxX, int maxY) : this(new(minX, minY), new(maxX, maxY)) { }

    public override readonly int GetHashCode() => HashCode.Combine(Min, Max);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Bound2DInt other && Equals(other);
    public readonly bool Equals(Bound2DInt other) => Min == other.Min && Max == other.Max;

    public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("G", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{Min.ToString(format, provider)}{separator} {Max.ToString(format, provider)}>";
    }

    public static bool operator ==(Bound2DInt left, Bound2DInt right) => left.Equals(right);
    public static bool operator !=(Bound2DInt left, Bound2DInt right) => !left.Equals(right);
}