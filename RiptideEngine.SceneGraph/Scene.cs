namespace RiptideEngine.SceneGraph;

internal enum SceneInternalFlags {
    None = 0,

    IsDestroyed = 1 << 0,
    IsResource = 1 << 1,
}

public sealed class Scene : RiptideObject, IResourceAsset {
    private readonly List<Entity> _roots;
    private SceneInternalFlags _internalFlags;

    public bool IsDestroyed => _internalFlags.HasFlag(SceneInternalFlags.IsDestroyed);
    public bool IsResourceAsset => _internalFlags.HasFlag(SceneInternalFlags.IsResource);

    internal List<Entity> RootEntities => _roots;
    public int RootEntityCount => _roots.Count;

    internal Scene() {
        _roots = [];
    }

    public IEnumerable<Entity> EnumerateRootEntities() => _roots;

    public Entity CreateEntity() {
        var entity = new Entity();
        entity.Preinitialize(this);
        _roots.Add(entity);

        return entity;
    }

    public Entity CreateEntity(Entity? Parent) {
        if (Parent != null && Parent.Scene != this) throw new ArgumentException("Parent's scene is not the scene that called CreateEntity.", nameof(Parent));

        var entity = new Entity();
        entity.Preinitialize(this);

        if (Parent == null) {
            _roots.Add(entity);
        } else {
            entity.SetParent(Parent);
        }

        return entity;
    }

    internal void AddDeserializedEntity(Entity entity) {
        entity.Preinitialize(this);
        _roots.Add(entity);
    }

    internal void AddEntity(Entity entity) {
        _roots.Add(entity);
    }

    public Entity GetEntityAt(int index) => _roots[index];
    public int GetIndexOfEntity(Entity entity) => _roots.IndexOf(entity);

    public void Tick() {
        foreach (var entity in _roots) {
            CallUpdateRecursive(entity);
        }

        static void CallUpdateRecursive(Entity entity) {
            if (entity.IsDestroyed) return;

            foreach (var component in entity.EnumerateComponents<ILifecycleUpdate>()) {
                component.Update();
            }

            foreach (var child in entity.EnumerateChildren()) {
                CallUpdateRecursive(child);
            }
        }
    }

    internal void Destroy() {
        if (IsDestroyed) return;

        foreach (var entity in _roots) {
            entity.Destroy();
        }

        _roots.Clear();
        _internalFlags |= SceneInternalFlags.IsDestroyed;
    }

    public bool CanInstantiate<T>() => typeof(T) == typeof(Scene);
    public bool CanInstantiate(Type type) => type == typeof(Scene);

    public bool TryInstantiate<T>([NotNullWhen(true)] out T? output) {
        if (typeof(T) != typeof(Scene)) {
            output = default;
            return false;
        }

        output = (T)(object)InstantiateScene();
        return true;
    }

    public bool TryInstantiate(Type type, [NotNullWhen(true)] out object? output) {
        if (type != typeof(Scene)) {
            output = null;
            return false;
        }

        output = InstantiateScene();
        return true;
    }

    private Scene InstantiateScene() {
        Scene scene = new() {
            Name = Name,
            _internalFlags = SceneInternalFlags.None,
        };
        scene._roots.EnsureCapacity(_roots.Count);

        foreach (var root in _roots) {
            scene._roots.Add(root.Instantiate<Entity>()!);
        }

        return scene;
    }
}