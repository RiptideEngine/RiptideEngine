namespace RiptideEngine.Core;

public readonly struct IntegerRange<T>(T min, T max) : IEquatable<IntegerRange<T>> where T : IBinaryInteger<T> {
    public readonly T Min = min;
    public readonly T Max = max;

    public IntegerRange(T max) : this(T.Zero, max) { }

    [Pure]
    public void Deconstruct(out T min, out T max) {
        min = Min;
        max = Max;
    }
    
    [Pure] public override int GetHashCode() => HashCode.Combine(Min, Max);
    
    [Pure] public override bool Equals(object? obj) => obj is IntegerRange<T> other && Equals(other);
    [Pure] public bool Equals(IntegerRange<T> other) => Min == other.Min && Max == other.Max;

    [Pure] public override string ToString() => $"<{Min}, {Max}>";
    [Pure] public string ToString(string? format, IFormatProvider? provider) => $"<{Min.ToString(format, provider)}, {Max.ToString(format, provider)}>";
}