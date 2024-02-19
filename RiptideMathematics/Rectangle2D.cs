namespace RiptideMathematics;

public partial struct Rectangle2D(Vector2 position, Vector2 size) : IEquatable<Rectangle2D>, IFormattable {
    public Vector2 Position = position;
    public Vector2 Size = size;

    public readonly float Area => Size.X * Size.Y;
    public readonly float Perimeter => 2 * (Size.X + Size.Y);
    public readonly Vector2 Center => Position + Size / 2;

    public Rectangle2D(float x, float y, Vector2 size) : this(new(x, y), size) { }
    public Rectangle2D(Vector2 position, float width, float height) : this(position, new(width, height)) { }
    public Rectangle2D(float x, float y, float width, float height) : this(new(x, y), new(width, height)) { }

    public void Deconstruct(out Vector2 position, out Vector2 size) {
        position = Position;
        size = Size;
    }

    public void Deconstruct(out float x, out float y, out float width, out float height) {
        x = Position.X;
        y = Position.Y;
        width = Size.X;
        height = Size.Y;
    }

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