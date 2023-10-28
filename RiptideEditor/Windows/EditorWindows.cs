namespace RiptideEditor.Windows;

public static class EditorWindows {
    private static readonly List<EditorWindow> _windows = [];

    public static T AddWindowInstance<T>() where T : EditorWindow, new() {
        var instance = new T();
        _windows.Add(instance);

        instance.Initialize();

        return instance;
    }

    public static T GetOrAddWindowInstance<T>() where T : EditorWindow, new() {
        if (TryGetFirstWindow<T>(out var window)) return window;
        return AddWindowInstance<T>();
    }

    public static bool TryGetFirstWindow<T>([NotNullWhen(true)] out T? outputWindow) where T : EditorWindow {
        foreach (var window in _windows) {
            if (window is T t) {
                outputWindow = t;
                return true;
            }
        }

        outputWindow = null;
        return false;
    }

    public static IEnumerable<EditorWindow> GetWindows() => _windows;
    public static IEnumerable<T> EnumerateWindow<T>() where T : EditorWindow => _windows.OfType<T>();

    internal static void RemoveAllWindows() {
        foreach (var window in _windows) {
            window.Dispose();
        }
        _windows.Clear();
    }

    public static bool RemoveWindow<T>() where T : EditorWindow {
        for (int i = 0; i < _windows.Count; i++) {
            if (_windows[i] is T window) {
                window.Dispose();
                _windows.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public static void RemoveWindows<T>() where T : EditorWindow {
        for (int i = 0; i < _windows.Count;) {
            if (_windows[i] is T window) {
                window.Dispose();
                _windows.RemoveAt(i);
                continue;
            }

            i++;
        }
    }

    internal static void RenderWindows() {
        for (int i = 0; i < _windows.Count;) {
            var window = _windows[i];

            if (!window.Render()) {
                window.Dispose();
                _windows.RemoveAt(i);
                continue;
            }

            i++;
        }
    }
}