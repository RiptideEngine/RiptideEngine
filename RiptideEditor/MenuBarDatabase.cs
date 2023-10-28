namespace RiptideEditor;

internal static class MenuBarDatabase {
    public readonly record struct Entry(string Path, string Shortcut, Action Callback, Func<bool>? VisibilityCallback);

    private static ImmutableArray<ImmutableArray<Entry>> _entries;

    static MenuBarDatabase() {
        _entries = [];
    }

    public static void Reload() {
        var builders = ImmutableArray.CreateBuilder<ImmutableArray<Entry>.Builder>(MenuBarSectionExtensions.UniqueValueCount);

        for (int i = 0; i < MenuBarSectionExtensions.UniqueValueCount; i++) {
            builders.Add(ImmutableArray.CreateBuilder<Entry>());
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            foreach (var type in assembly.GetTypes()) {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)) {
                    if (method.ReturnType != typeof(void)) continue;
                    if (method.GetParameters().Length != 0) continue;

                    var attribute = method.GetCustomAttribute<MenuBarCallbackAttribute>();
                    if (attribute == null) continue;
                    if (!attribute.Section.IsDefined()) continue;

                    var path = attribute.Path;
                    if (path == null) continue;

                    if (path.StartsWith('/') || path.EndsWith('/')) continue;

                    var callback = (Action)Delegate.CreateDelegate(typeof(Action), method);

                    var builder = builders[(int)attribute.Section];

                    if (attribute.VisibilityMethod != null && type.GetMethod(attribute.VisibilityMethod, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy, Type.EmptyTypes) is { } visibilityMethod) {
                        if (visibilityMethod.ReturnType == typeof(bool)) {
                            builder.Add(new(path, attribute.Shortcut ?? string.Empty, callback, (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), visibilityMethod)));
                            continue;
                        }
                    }

                    builder.Add(new(path, attribute.Shortcut ?? string.Empty, callback, null));
                }
            }
        }

        var entryBuilder = ImmutableArray.CreateBuilder<ImmutableArray<Entry>>(MenuBarSectionExtensions.UniqueValueCount);
        for (int i = 0; i < MenuBarSectionExtensions.UniqueValueCount; i++) {
            entryBuilder.Add(builders[i].DrainToImmutable());
        }

        _entries = entryBuilder.MoveToImmutable();
    }

    public static ImmutableArray<Entry> GetEntries(MenuBarSection section) {
        if (!section.IsDefined()) return [];

        return _entries[(int)section];
    }
}