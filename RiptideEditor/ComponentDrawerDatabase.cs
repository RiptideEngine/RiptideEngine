namespace RiptideEditor;

internal static class ComponentDrawerDatabase {
    private static readonly Dictionary<Type, Type> _drawerTypes;

    static ComponentDrawerDatabase() {
        _drawerTypes = new();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            ImportFromAssembly(assembly);
        }
    }

    private static void ImportFromAssembly(Assembly assembly) {
        var subclass = typeof(BaseComponentDrawer);
        var attributeType = typeof(ComponentDrawerAttribute<>);

        foreach (var type in assembly.GetTypes()) {
            if (!type.IsSubclassOf(subclass)) continue;

            var attribute = type.GetCustomAttribute(attributeType);

            if (attribute == null) return;

            var nodeType = attribute.GetType().GetGenericArguments()[0];

            ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(_drawerTypes, nodeType, out bool exists);

            if (exists) {
                continue;
            }

            value = type;
        }
    }

    public static bool TryCreateDrawer(Type nodeType, [NotNullWhen(true)] out BaseComponentDrawer? drawer) {
        if (!_drawerTypes.TryGetValue(nodeType, out var drawerType)) {
            drawer = null;
            return false;
        }

        drawer = (BaseComponentDrawer)Activator.CreateInstance(drawerType)!;
        return true;
    }
}