namespace RiptideMathematics;

public partial struct Vector2Int(int x, int y) : IEquatable<Vector2Int>, IFormattable {
    public static Vector2Int Zero => default;
    public static Vector2Int UnitX => new(1, 0);
    public static Vector2Int UnitY => new(0, 1);
    public static Vector2Int One => new(1, 1);

    public int X = x, Y = y;

    public Vector2Int(int value) : this(value, value) { }

    public override readonly int GetHashCode() => HashCode.Combine(X, Y);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Vector2Int other && Equals(other);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Vector2Int other) => X == other.X && Y == other.Y;

    public override readonly string ToString() => ToString("D", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("D", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{X.ToString(format, provider)}{separator} {Y.ToString(format, provider)}>";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector2(Vector2Int vector) => new(vector.X, vector.Y);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector2Int(Vector2 vector) => new((int)vector.X, (int)vector.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator +(Vector2Int left, Vector2Int right) => Add(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator -(Vector2Int left, Vector2Int right) => Subtract(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator -(Vector2Int value) => Negate(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator *(Vector2Int left, Vector2Int right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator *(Vector2Int left, int right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator *(int left, Vector2Int right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator /(Vector2Int left, Vector2Int right) => Divide(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator /(Vector2Int left, int right) => Divide(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator /(Vector2Int left, float right) => Divide(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator &(Vector2Int left, Vector2Int right) => And(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator |(Vector2Int left, Vector2Int right) => Or(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator ^(Vector2Int left, Vector2Int right) => Xor(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator ~(Vector2Int value) => Complement(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector2Int left, Vector2Int right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector2Int left, Vector2Int right) => !left.Equals(right);
}