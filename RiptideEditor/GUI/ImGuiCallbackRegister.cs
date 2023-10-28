namespace RiptideEditor;

public static class ImGuiCallbackRegister {
    public delegate void Callback(object param);
    public readonly record struct Entry(Callback Callback, object Parameter);

    private static nint _callbackIndex;
    private static readonly Dictionary<nint, Entry> _storage;

    static ImGuiCallbackRegister() {
        _callbackIndex = 0;
        _storage = new();
    }

    public static void ClearAllCallbacks() {
        _storage.Clear();
        _callbackIndex = 0;
    }

    public static nint AddCallback(Callback callback, object parameter) {
        _storage.Add(++_callbackIndex, new(callback, parameter));
        return _callbackIndex;
    }

    public static bool RemoveCallback(nint id) => _storage.Remove(id);

    public static bool TryGetCallback(nint id, out Entry output) => _storage.TryGetValue(id, out output);
}