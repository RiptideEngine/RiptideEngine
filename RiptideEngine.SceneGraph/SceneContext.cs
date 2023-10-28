namespace RiptideEngine.SceneGraph;

public sealed class SceneContext {
    private readonly List<Scene> _scenes;

    public event Action<Scene>? SceneAddedCallback;
    public event Action<Scene>? SceneRemoveCallback;

    public int SceneCount => _scenes.Count;

    public SceneContext() {
        _scenes = [];
    }

    public Scene CreateScene() {
        var scene = new Scene();
        _scenes.Add(scene);

        SceneAddedCallback?.Invoke(scene);

        return scene;
    }

    // Obsolete once System.Text.Json have an official way to populate data into exists object.
    // "But you can use DTO objects". **no**
    internal void AddScene(Scene scene) {
        _scenes.Add(scene);
        SceneAddedCallback?.Invoke(scene);
    }

    public bool TryGetScene(string name, [NotNullWhen(true)] out Scene? output) {
        foreach (var scene in _scenes) {
            if (scene.Name == name) {
                output = scene;
                return true;
            }
        }

        output = null;
        return false;
    }

    public Scene this[int index] => _scenes[index];

    public IEnumerable<Scene> EnumerateScenes() => _scenes;

    public bool RemoveScene(int index) {
        if (index < 0 || index >= _scenes.Count) return false;

        _scenes[index].Destroy();
        _scenes.RemoveAt(index);

        SceneRemoveCallback?.Invoke(_scenes[index]);

        return true;
    }

    public bool RemoveScene(Scene scene) {
        var index = _scenes.IndexOf(scene);
        if (index == -1) return false;

        _scenes[index].Destroy();
        _scenes.RemoveAt(index);
        SceneRemoveCallback?.Invoke(scene);

        return true;
    }

    public bool RemoveScene(string name) {
        for (int i = 0; i < _scenes.Count; i++) {
            if (_scenes[i].Name == name) {
                _scenes.RemoveAt(i);
                SceneRemoveCallback?.Invoke(_scenes[i]);

                return true;
            }
        }

        return false;
    }

    public void Clear(bool invokeCallback) {
        if (invokeCallback && SceneRemoveCallback != null) {
            foreach (var scene in _scenes) {
                SceneRemoveCallback.Invoke(scene);
            }
        }

        _scenes.Clear();
    }

    public bool ContainsScene(Scene scene) => _scenes.Contains(scene);
}