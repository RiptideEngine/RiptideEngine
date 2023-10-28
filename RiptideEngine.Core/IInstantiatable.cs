namespace RiptideEngine.Core;

/// <summary>
/// Interface that allow classes instantiate a separate clone of itself, allows for finer control of output type and performance.
/// </summary>
public interface IInstantiatable {
    bool CanInstantiate<T>();
    bool CanInstantiate(Type outputType);

    bool TryInstantiate<T>([NotNullWhen(true)] out T? output);
    bool TryInstantiate(Type outputType, [NotNullWhen(true)] out object? output);
}

public static class InstantiatableExtension {
    public static T? Instantiate<T>(this IInstantiatable instantiatable) => instantiatable.TryInstantiate<T>(out var output) ? output : default;
    public static object? Instantiate(this IInstantiatable instantiatable, Type outputType) => instantiatable.TryInstantiate(outputType, out var output) ? output : default;
}