namespace RiptideMathematics;

public partial struct Vector2UInt(uint x, uint y) : IEquatable<Vector2UInt>, 
                                                    IFormattable, 
                                                    IAdditionOperators<Vector2UInt, Vector2UInt, Vector2UInt>, 
                                                    ISubtractionOperators<Vector2UInt, Vector2UInt, Vector2UInt>, 
                                                    IMultiplyOperators<Vector2UInt, Vector2UInt, Vector2UInt>, 
                                                    IMultiplyOperators<Vector2UInt, uint, Vector2UInt>,
                                                    IMultiplyOperators<Vector2UInt, float, Vector2>,
                                                    IDivisionOperators<Vector2UInt, Vector2UInt, Vector2UInt>, 
                                                    IDivisionOperators<Vector2UInt, uint, Vector2UInt>, 
                                                    IDivisionOperators<Vector2UInt, float, Vector2>, 
                                                    IBitwiseOperators<Vector2UInt, Vector2UInt, Vector2UInt>,
                                                    IEqualityOperators<Vector2UInt, Vector2UInt, bool>,
                                                    IAdditiveIdentity<Vector2UInt, Vector2UInt>,
                                                    IMultiplicativeIdentity<Vector2UInt, Vector2UInt>
{
    public static Vector2UInt Zero => default;
    public static Vector2UInt UnitX => new(1, 0);
    public static Vector2UInt UnitY => new(0, 1);
    public static Vector2UInt One => new(1, 1);

    public static Vector2UInt AdditiveIdentity => Zero;
    public static Vector2UInt MultiplicativeIdentity => One;

    public uint X = x, Y = y;

    public Vector2UInt(uint value) : this(value, value) { }

    public override readonly int GetHashCode() => HashCode.Combine(X, Y);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Vector2UInt other && Equals(other);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Vector2UInt other) => X == other.X && Y == other.Y;

    public override readonly string ToString() => ToString("D", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("D", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;
        
        return $"<{X.ToString(format, provider)}{separator} {Y.ToString(format, provider)}>";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector2(Vector2UInt vector) => new(vector.X, vector.Y);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector2UInt(Vector2 vector) => new((uint)vector.X, (uint)vector.Y);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector2UInt(Vector2Int vector) => new((uint)vector.X, (uint)vector.Y);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt operator +(Vector2UInt left, Vector2UInt right) => Add(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt operator -(Vector2UInt left, Vector2UInt right) => Subtract(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt operator *(Vector2UInt left, Vector2UInt right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt operator *(Vector2UInt left, uint right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator *(Vector2UInt left, float right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt operator *(uint left, Vector2UInt right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt operator /(Vector2UInt left, Vector2UInt right) => Divide(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt operator /(Vector2UInt left, uint right) => Divide(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator /(Vector2UInt left, float right) => Divide(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt operator &(Vector2UInt left, Vector2UInt right) => And(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt operator |(Vector2UInt left, Vector2UInt right) => Or(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt operator ^(Vector2UInt left, Vector2UInt right) => Xor(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt operator ~(Vector2UInt value) => Complement(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector2UInt left, Vector2UInt right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector2UInt left, Vector2UInt right) => !left.Equals(right);
}