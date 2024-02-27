namespace RiptideEngine.Core;

public readonly struct Optional<T> : IEquatable<Optional<T>> {
    public static Optional<T> Null => new(default, false);

    private readonly T? _value;
    public readonly bool HasValue;

    [MemberNotNullWhen(true, nameof(HasValue))] public T? Value => Get();

    private Optional(T? value, bool hasValue) {
        _value = value;
        HasValue = hasValue;
    }

    public bool TryGet([NotNullWhen(true)] out T? result) {
        result = _value;
        return HasValue;
    }

    public T Get() {
        if (!HasValue) throw new InvalidOperationException(ExceptionMessages.OptionalHasNoValue);

        return _value!;
    }

    public T? GetUnchecked() => _value;

    public bool Equals(Optional<T> other) {
        if (HasValue) {
            return other.HasValue && EqualityComparer<T>.Default.Equals(_value, other._value);
        }
        
        return !other.HasValue;
    }

    public override int GetHashCode() => typeof(T).IsValueType ? HasValue ? _value!.GetHashCode() : 0 : _value == null ? 0 : _value!.GetHashCode();
    public override string? ToString() => typeof(T).IsValueType ? HasValue ? _value!.ToString() : string.Empty : _value == null ? string.Empty : _value!.ToString();

    public static Optional<T> From(T? result) {
        if (typeof(T).IsValueType) {
            return new(result, true);
        }
        
        return new(result, result != null);
    }
    public static implicit operator Optional<T>(T? value) => From(value);
}