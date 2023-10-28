namespace RiptideFoundation;

internal sealed class ComponentDatabaseService : IComponentDatabase {
    private readonly Dictionary<Guid, Type> _guidToType;
    private readonly Dictionary<Type, Guid> _typeToGuid;

    public int ComponentsCount => _guidToType.Count;

    public ComponentDatabaseService() {
        _guidToType = [];
        _typeToGuid = [];

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            LoadComponentTypes(assembly, _guidToType, _typeToGuid);
        }
    }

    private static void LoadComponentTypes(Assembly assembly, Dictionary<Guid, Type> receiver1, Dictionary<Type, Guid> receiver2) {
        var ctype = typeof(Component);

        foreach (var type in assembly.GetTypes()) {
            if (type.IsAbstract || type.ContainsGenericParameters) continue;
            if (!type.IsSubclassOf(ctype)) continue;

            // TODO: Invalid usage reporting.

            var attribute = type.GetCustomAttribute<EntityComponentAttribute>();
            if (attribute == null) continue;

            if (!Guid.TryParse(attribute.Guid, out var guid)) continue;

            receiver1.TryAdd(guid, type);
            receiver2.TryAdd(type, guid);
        }
    }

    public bool ContainsComponent(Guid guid) => _guidToType.ContainsKey(guid);
    public bool TryGetComponentType(Guid guid, [NotNullWhen(true)] out Type? type) => _guidToType.TryGetValue(guid, out type);

    public bool TryGetComponentGuid(Type type, out Guid guid) => _typeToGuid.TryGetValue(type, out guid);

    public IEnumerable<KeyValuePair<Guid, Type>> EnumerateComponentTypes() => _guidToType;
    public IEnumerable<KeyValuePair<Type, Guid>> EnumerateComponentGuids() => _typeToGuid;

    public void Dispose() {
        
    }
}