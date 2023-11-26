namespace RiptideMathematics;

public partial struct Vector3Int(int x, int y, int z) : IEquatable<Vector3Int>, IFormattable {
    public static Vector3Int Zero => default;
    public static Vector3Int UnitX => new(1, 0, 0);
    public static Vector3Int UnitY => new(0, 1, 0);
    public static Vector3Int UnitZ => new(0, 0, 1);
    public static Vector3Int One => new(1, 1, 1);

    public int X = x, Y = y, Z = z;

    public Vector3Int(int value) : this(value, value, value) { }

    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Vector3Int other && Equals(other);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Vector3Int other) => X == other.X && Y == other.Y && Z == other.Z;

    public override readonly string ToString() => ToString("D", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("D", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;

        return $"<{X.ToString(format, provider)}{separator} {Y.ToString(format, provider)}{separator} {Z.ToString(format, provider)}>";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector3(Vector3Int vector) => new(vector.X, vector.Y, vector.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector3Int(Vector3 vector) => new((int)vector.X, (int)vector.Y, (int)vector.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator +(Vector3Int left, Vector3Int right) => Add(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator -(Vector3Int left, Vector3Int right) => Subtract(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator -(Vector3Int value) => Negate(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator *(Vector3Int left, Vector3Int right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator *(Vector3Int left, int right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator *(int left, Vector3Int right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator /(Vector3Int left, Vector3Int right) => Divide(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator /(Vector3Int left, int right) => Divide(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator /(Vector3Int left, float right) => Divide(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator &(Vector3Int left, Vector3Int right) => And(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator |(Vector3Int left, Vector3Int right) => Or(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator ^(Vector3Int left, Vector3Int right) => Xor(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int operator ~(Vector3Int value) => Complement(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector3Int left, Vector3Int right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector3Int left, Vector3Int right) => !left.Equals(right);
}