namespace RiptideEngine.Core;

/// <summary>
/// A class responsible to encapsulate a Dictionary of services that can be created and retrieved via interface types.
/// </summary>
public sealed class RiptideServices {
    private readonly Dictionary<Type, IRiptideService> _services;

    public RiptideServices() {
        _services = [];
    }

    public TType CreateService<TInterface, TType>() where TInterface : IRiptideService
                                                    where TType : TInterface, new() {
        var interfaceType = typeof(TInterface);
        var instanceType = typeof(TType);

        if (!interfaceType.IsInterface) throw new ArgumentException(string.Format(ExceptionMessages.GenericArgumentMustBeInterface, nameof(TInterface)));
        if (_services.ContainsKey(interfaceType)) throw new ArgumentException(string.Format(ExceptionMessages.ServiceAlreadyRegistered, interfaceType.FullName));

        var instance = new TType();
        _services.Add(interfaceType, instance);

        return instance;
    }

    public TType CreateService<TInterface, TType, TOptions>(TOptions options) where TInterface : IRiptideService
                                                                              where TType : class, TInterface
                                                                              where TOptions : notnull {
        var interfaceType = typeof(TInterface);
        var instanceType = typeof(TType);

        if (!interfaceType.IsInterface) throw new ArgumentException(string.Format(ExceptionMessages.GenericArgumentMustBeInterface, nameof(TInterface)));
        if (_services.ContainsKey(interfaceType)) throw new ArgumentException(string.Format(ExceptionMessages.ServiceAlreadyRegistered, interfaceType.FullName));

        var optionType = typeof(TOptions);

        var constructor = instanceType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, [optionType]) ?? throw new ArgumentException($"Failed to get constructor of '{instanceType.FullName}' that takes 1 parameter of option type '{optionType.FullName}'.");
        var instance = Unsafe.As<TType>(constructor.Invoke(new object[] { options }));
        _services.Add(interfaceType, instance);

        return instance;
    }

    /// <summary>
    /// Try retrieve the service instance that has been created before.
    /// </summary>
    /// <typeparam name="TInterface">Service interface type to retrieve instance from.</typeparam>
    /// <param name="output">Output parameter contains retrieved service type if retrieve successful.</param>
    /// <returns><see langword="true"/> if service interface type has already been registered before via <see cref="CreateService{TInterface, TType}"/>, <see langword="false"/> otherwise.</returns>
    public bool TryGetService<TInterface>([NotNullWhen(true)] out TInterface? output) where TInterface : IRiptideService {
        if (_services.TryGetValue(typeof(TInterface), out var obj)) {
            output = (TInterface)obj;
            return true;
        }

        output = default;
        return false;
    }

    /// <summary>
    /// Retrieve the service instance that has been created before.
    /// </summary>
    /// <typeparam name="TInterface">Service interface type to retrieve instance from.</typeparam>
    /// <returns>Retrieved service that implements <typeparamref name="TInterface"/>.</returns>
    public TInterface? GetService<TInterface>() where TInterface : IRiptideService {
        TryGetService<TInterface>(out var output);
        return output;
    }

    public TInterface GetRequiredService<TInterface>() where TInterface : IRiptideService {
        if (TryGetService<TInterface>(out var output)) return output;

        throw new InvalidOperationException(string.Format(ExceptionMessages.MissingRequiredService, typeof(TInterface).FullName));
    }

    /// <summary>
    /// Remove registered service instance and dispose it.
    /// </summary>
    /// <typeparam name="TInterface">Service interface type to remove from.</typeparam>
    /// <returns><see langword="true"/> if service instance has been removed, <see langword="false"/> otherwise.</returns>
    public bool RemoveService<TInterface>() where TInterface : IRiptideService {
        if (_services.Remove(typeof(TInterface), out var obj)) {
            obj.Dispose();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Remove all registered services.
    /// </summary>
    public void RemoveAllServices() {
        foreach (var service in _services.Values) {
            service.Dispose();
        }

        _services.Clear();
    }
}