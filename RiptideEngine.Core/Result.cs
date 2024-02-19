namespace RiptideEngine.Core;

public readonly struct Result<TResult, TError> {
    private readonly TResult? _value;
    private readonly TError? _error;

    public readonly bool IsError;
    
    private Result(TResult? value, TError? error, bool isError) {
        _value = value;
        _error = error;
        IsError = isError;
    }

    public bool TryGet([NotNullWhen(true)] out TResult? result) {
        result = _value;
        return !IsError;
    }

    public TResult GetValue() {
        if (IsError) throw new InvalidOperationException(ExceptionMessages.ResultHasNoValue);

        return _value!;
    }

    public TResult? GetValueUnchecked() => _value;

    public static Result<TResult, TError> Success(TResult result) => new(result, default, false);
    public static Result<TResult, TError> Failure(TError error) => new(default, error, true);
}