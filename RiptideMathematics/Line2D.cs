namespace RiptideMathematics;

public struct Line2D(Vector2 position, Vector2 direction) : IEquatable<Line2D> {
    public Vector2 Position = position;
    public Vector2 Direction = Vector2.Normalize(direction);

    public Line2D(float x, float y, Vector2 direction) : this(new(x, y), direction) { }
    public Line2D(Vector2 position, float dirX, float dirY) : this(position, new(dirX, dirY)) { }
    public Line2D(float x, float y, float dirX, float dirY) : this(new(x, y), new(dirX, dirY)) { }

    public Vector2 GetPosition(float distance) => Position + Direction * distance;

    public void Deconstruct(out Vector2 position, out Vector2 direction) {
        position = Position;
        direction = Direction;
    }

    public void Deconstruct(out float x, out float y, out float dirX, out float dirY) {
        x = Position.X;
        y = Position.Y;
        dirX = Direction.X;
        dirY = Direction.Y;
    }

    public Ray2D ToRay() => Unsafe.BitCast<Line2D, Ray2D>(this);

    public override readonly int GetHashCode() => HashCode.Combine(Position, Direction);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Ray2D other && Equals(other);
    public readonly bool Equals(Line2D other) => Unsafe.BitCast<Line2D, Vector4>(this) == Unsafe.BitCast<Line2D, Vector4>(other);

    public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("G", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{Position.ToString(format, provider)}{separator} {Direction.ToString(format, provider)}>";
    }

    public static Line2D CreateWithoutNormalize(Vector2 position, Vector2 direction) => new() {
        Position = position,
        Direction = direction,
    };

    public static bool operator ==(Line2D left, Line2D right) => left.Equals(right);
    public static bool operator !=(Line2D left, Line2D right) => !left.Equals(right);
}