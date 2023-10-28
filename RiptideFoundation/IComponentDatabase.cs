namespace RiptideFoundation;

public interface IComponentDatabase : IRiptideService {
    int ComponentsCount { get; }

    bool ContainsComponent(Guid guid);
    bool TryGetComponentType(Guid guid, [NotNullWhen(true)] out Type? type);

    bool TryGetComponentGuid(Type componentType, out Guid guid);

    IEnumerable<KeyValuePair<Guid, Type>> EnumerateComponentTypes();
    IEnumerable<KeyValuePair<Type, Guid>> EnumerateComponentGuids();
}