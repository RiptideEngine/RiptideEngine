namespace RiptideMathematics;

public struct Ray2D(Vector2 position, Vector2 direction) : IEquatable<Ray2D> {
    public Vector2 Position = position;
    public Vector2 Direction = Vector2.Normalize(direction);

    public Ray2D(float x, float y, Vector2 direction) : this(new(x, y), direction) { }
    public Ray2D(Vector2 position, float dirX, float dirY) : this(position, new(dirX, dirY)) { }
    public Ray2D(float x, float y, float dirX, float dirY) : this(new(x, y), new(dirX, dirY)) { }

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

    public override readonly int GetHashCode() => HashCode.Combine(Position, Direction);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Ray2D other && Equals(other);
    public readonly bool Equals(Ray2D other) => Unsafe.BitCast<Ray2D, Vector4>(this) == Unsafe.BitCast<Ray2D, Vector4>(other);

    public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("G", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{Position.ToString(format, provider)}{separator} {Direction.ToString(format, provider)}>";
    }

    public static Ray2D CreateWithoutNormalize(Vector2 position, Vector2 direction) => new() {
        Position = position,
        Direction = direction,
    };

    public static bool operator ==(Ray2D left, Ray2D right) => left.Equals(right);
    public static bool operator !=(Ray2D left, Ray2D right) => !left.Equals(right);
}