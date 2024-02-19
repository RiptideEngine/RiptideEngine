namespace RiptideEngine.Core;

public readonly struct Optional<T> {
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

    public static Optional<T> From(T? result) {
        if (typeof(T).IsValueType) {
            return new(result, true);
        }
        
        return new(result, result != null);
    }

    public static implicit operator Optional<T>(T? value) => From(value);
}