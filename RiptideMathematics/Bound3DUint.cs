namespace RiptideMathematics;

public partial struct Bound3DUInt(Vector3UInt min, Vector3UInt max) : IEquatable<Bound3DUInt>, IFormattable {
    public Vector3UInt Min = min;
    public Vector3UInt Max = max;

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

    public readonly Vector3UInt Size => Max - Min;

    public Bound3DUInt(uint minX, uint minY, uint minZ, uint maxX, uint maxY, uint maxZ) : this(new(minX, minY, minZ), new(maxX, maxY, maxZ)) { }

    public override readonly int GetHashCode() => HashCode.Combine(Min, Max);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Bound3DUInt other && Equals(other);
    public readonly bool Equals(Bound3DUInt other) => Min == other.Min && Max == other.Max;

    public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("G", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{Min.ToString(format, provider)}{separator} {Max.ToString(format, provider)}>";
    }

    public static bool operator ==(Bound3DUInt left, Bound3DUInt right) => left.Equals(right);
    public static bool operator !=(Bound3DUInt left, Bound3DUInt right) => !left.Equals(right);
}