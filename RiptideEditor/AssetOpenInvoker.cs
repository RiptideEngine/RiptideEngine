namespace RiptideEditor;

internal static class AssetOpenInvoker {
    private static readonly Dictionary<int, List<(int Order, Action<string>)>> _dict;

    static AssetOpenInvoker() {
        _dict = [];
        
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            foreach (var type in assembly.GetTypes()) {
                if (type.ContainsGenericParameters) continue;

                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
                    if (method.ContainsGenericParameters) continue;
                    if (method.GetCustomAttribute<AssetOpenAttribute>() is not { } attribute) continue;

                    if (string.IsNullOrEmpty(attribute.AssetExtension)) continue;

                    var parameters = method.GetParameters();
                    if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string)) continue;

                    Register(method, attribute);
                }
            }
        }

        foreach ((_, var list) in _dict) {
            list.Sort(Reorder);
        }

        static int Reorder((int, Action<string>) a, (int, Action<string>) b) {
            return a.Item1.CompareTo(b.Item1);
        }
        static void Register(MethodInfo method, AssetOpenAttribute attribute) {
            Span<char> lowercase = attribute.AssetExtension.Length >= 16 ? new char[attribute.AssetExtension.Length] : stackalloc char[attribute.AssetExtension.Length];
            attribute.AssetExtension.AsSpan().ToLower(lowercase, null);

            ref var cacheList = ref CollectionsMarshal.GetValueRefOrAddDefault(_dict, string.GetHashCode(lowercase), out bool exists);
            if (!exists) cacheList = [];

            cacheList!.Add((attribute.Order, (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), method)));
        }
    }

    public static void Invoke(string resourcePath) {
        ReadOnlySpan<char> extension = Path.GetExtension(resourcePath.AsSpan());
        Span<char> lowercase = extension.Length >= 16 ? new char[extension.Length] : stackalloc char[extension.Length];
        extension.ToLower(lowercase, null);

        if (_dict.TryGetValue(string.GetHashCode(lowercase), out var cache)) {
            foreach ((_, var del) in cache) {
                del.Invoke(resourcePath);
            }
        }
    }
}