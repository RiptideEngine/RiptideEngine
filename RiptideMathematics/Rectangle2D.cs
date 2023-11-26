namespace RiptideMathematics;

public partial struct Rectangle2D(Vector2 position, Vector2 size) : IEquatable<Rectangle2D>, IFormattable {
    public Vector2 Position = position;
    public Vector2 Size = size;

    public readonly float Area => Size.X * Size.Y;
    public readonly float Perimeter => 2 * (Size.X + Size.Y);
    public readonly Vector2 Center => Position + Size / 2;

    public Rectangle2D(float px, float py, float sx, float sy) : this(new(px, py), new(sx, sy)) { }

    public override readonly int GetHashCode() => HashCode.Combine(Position, Size);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Rectangle2D other && Equals(other);
    public readonly bool Equals(Rectangle2D other) => Unsafe.BitCast<Rectangle2D, Vector4>(this) == Unsafe.BitCast<Rectangle2D, Vector4>(other);

    public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("G", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{Position.ToString(format, provider)}{separator} {Size.ToString(format, provider)}>";
    }

    public static implicit operator Bound2D(Rectangle2D rect) => new(rect.Position, rect.Position + rect.Size);

    public static bool operator ==(Rectangle2D left, Rectangle2D right) => left.Equals(right);
    public static bool operator !=(Rectangle2D left, Rectangle2D right) => !left.Equals(right);
}