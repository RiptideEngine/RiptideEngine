namespace RiptideMathematics;

public partial struct Bound2D(Vector2 min, Vector2 max) : IEquatable<Bound2D>, IFormattable {
    public static Bound2D Zero => default;
    
    public Vector2 Min = min, Max = max;

    public readonly float Area {
        get {
            var size = Max - Min;
            return size.X * size.Y;
        }
    }
    public readonly float Perimeter {
        get {
            var size = Max - Min;
            return 2 * (size.X + size.Y);
        }
    }

    public readonly Vector2 Size => Max - Min;

    public Bound2D(float minX, float minY, Vector2 max) : this(new(minX, minY), max) { }
    public Bound2D(Vector2 min, float maxX, float maxY) : this(min, new(maxX, maxY)) { }
    public Bound2D(float minX, float minY, float maxX, float maxY) : this(new(minX, minY), new(maxX, maxY)) { }

    public void Deconstruct(out Vector2 min, out Vector2 max) {
        min = Min;
        max = Max;
    }
    
    public void Deconstruct(out float minX, out float minY, out float maxX, out float maxY) {
        minX = Min.X;
        minY = Min.Y;
        maxX = Max.X;
        maxY = Max.Y;
    }

    public override readonly int GetHashCode() => HashCode.Combine(Min, Max);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Bound2D other && Equals(other);
    public readonly bool Equals(Bound2D other) => Unsafe.BitCast<Bound2D, Vector4>(this) == Unsafe.BitCast<Bound2D, Vector4>(other);

    public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("G", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{Min.ToString(format, provider)}{separator} {Max.ToString(format, provider)}>";
    }

    public static implicit operator Rectangle2D(Bound2D bound) => new(bound.Min, bound.Max - bound.Min);

    public static bool operator ==(Bound2D left, Bound2D right) => left.Equals(right);
    public static bool operator !=(Bound2D left, Bound2D right) => !left.Equals(right);
}