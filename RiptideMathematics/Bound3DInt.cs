namespace RiptideMathematics;

public partial struct Bound3DInt(Vector3Int min, Vector3Int max) : IEquatable<Bound3DInt>, IFormattable {
    public Vector3Int Min = min;
    public Vector3Int Max = max;

    public readonly float Area {
        get {
            var size = Size;
            return size.X * size.Y * size.Z;
        }
    }
    public readonly float Perimeter {
        get {
            var size = Size;
            return 2 * (size.X + size.Y + size.Z);
        }
    }

    public readonly Vector3Int Size => Max - Min;

    public Bound3DInt(int minX, int minY, int minZ, int maxX, int maxY, int maxZ) : this(new(minX, minY, minZ), new(maxX, maxY, maxZ)) { }

    public override readonly int GetHashCode() => HashCode.Combine(Min, Max);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Bound3DInt other && Equals(other);
    public readonly bool Equals(Bound3DInt other) => Min == other.Min && Max == other.Max;

    public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("G", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{Min.ToString(format, provider)}{separator} {Max.ToString(format, provider)}>";
    }

    public static bool operator ==(Bound3DInt left, Bound3DInt right) => left.Equals(right);
    public static bool operator !=(Bound3DInt left, Bound3DInt right) => !left.Equals(right);
}