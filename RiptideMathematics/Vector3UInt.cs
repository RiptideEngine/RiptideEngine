namespace RiptideMathematics;

public partial struct Vector3UInt(uint x, uint y, uint z) : IEquatable<Vector3UInt>,
                                                            IFormattable,
                                                            IAdditionOperators<Vector3UInt, Vector3UInt, Vector3UInt>, 
                                                            ISubtractionOperators<Vector3UInt, Vector3UInt, Vector3UInt>, 
                                                            IMultiplyOperators<Vector3UInt, Vector3UInt, Vector3UInt>, 
                                                            IMultiplyOperators<Vector3UInt, uint, Vector3UInt>, 
                                                            IMultiplyOperators<Vector3UInt, float, Vector3>, 
                                                            IDivisionOperators<Vector3UInt, Vector3UInt, Vector3UInt>, 
                                                            IDivisionOperators<Vector3UInt, uint, Vector3UInt>, 
                                                            IDivisionOperators<Vector3UInt, float, Vector3>, 
                                                            IBitwiseOperators<Vector3UInt, Vector3UInt, Vector3UInt>,
                                                            IEqualityOperators<Vector3UInt, Vector3UInt, bool>,
                                                            IAdditiveIdentity<Vector3UInt, Vector3UInt>,
                                                            IMultiplicativeIdentity<Vector3UInt, Vector3UInt>
{
    public static Vector3UInt Zero => default;
    public static Vector3UInt UnitX => new(1, 0, 0);
    public static Vector3UInt UnitY => new(0, 1, 0);
    public static Vector3UInt UnitZ => new(0, 0, 1);
    public static Vector3UInt One => new(1, 1, 1);

    public static Vector3UInt AdditiveIdentity => Zero;
    public static Vector3UInt MultiplicativeIdentity => One;

    public uint X = x, Y = y, Z = z;

    public Vector3UInt(uint value) : this(value, value, value) { }

    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Vector3UInt other && Equals(other);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Vector3UInt other) => X == other.X && Y == other.Y && Z == other.Z;

    public override readonly string ToString() => ToString("D", CultureInfo.CurrentCulture);
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public readonly string ToString(IFormatProvider? provider) => ToString("D", provider);
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;
        
        return $"<{X.ToString(format, provider)}{separator} {Y.ToString(format, provider)}>";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector3(Vector3UInt vector) => new(vector.X, vector.Y, vector.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector3UInt(Vector3 vector) => new((uint)vector.X, (uint)vector.Y, (uint)vector.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt operator +(Vector3UInt left, Vector3UInt right) => Add(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt operator -(Vector3UInt left, Vector3UInt right) => Subtract(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt operator *(Vector3UInt left, Vector3UInt right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt operator *(Vector3UInt left, uint right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator *(Vector3UInt left, float right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt operator *(uint left, Vector3UInt right) => Multiply(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt operator /(Vector3UInt left, Vector3UInt right) => Divide(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt operator /(Vector3UInt left, uint right) => Divide(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator /(Vector3UInt left, float right) => Divide(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt operator &(Vector3UInt left, Vector3UInt right) => And(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt operator |(Vector3UInt left, Vector3UInt right) => Or(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt operator ^(Vector3UInt left, Vector3UInt right) => Xor(left, right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt operator ~(Vector3UInt value) => Complement(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector3UInt left, Vector3UInt right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector3UInt left, Vector3UInt right) => !left.Equals(right);
}