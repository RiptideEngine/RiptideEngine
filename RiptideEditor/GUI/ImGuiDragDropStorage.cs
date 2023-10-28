namespace RiptideEditor;

/// <summary>
/// A class that provides service to store managed data that can be used for ImGui's drag and drop widgets.
/// </summary>
public static class ImGuiDragDropStorage {
    private static Dictionary<int, object> _values;

    static ImGuiDragDropStorage() {
        _values = new();
    }

    public static int Set(ReadOnlySpan<char> stringID, object value) {
        var id = string.GetHashCode(stringID);

        _values[id] = value;
        return id;
    }

    public static object? Get(int id) => _values.TryGetValue(id, out var value) ? value : null;

    public static bool TryGet(int id, [NotNullWhen(true)] out object? output) => _values.TryGetValue(id, out output);

    public static void Remove(int id) => _values.Remove(id);

    public static void Clear() => _values.Clear();
}