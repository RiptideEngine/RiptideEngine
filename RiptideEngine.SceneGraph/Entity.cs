namespace RiptideEngine.SceneGraph;

[Flags]
internal enum EntityInternalFlags {
    None = 0,

    Destroyed = 1 << 0,
    TransformDirty = 1 << 1,
}

public partial class Entity : RiptideObject {
    private EntityInternalFlags _internalFlags;

    public Scene Scene { get; internal set; } = null!;
    public uint ID { get; internal set; }

    internal Entity() {
        _children = [];
        _components = [];
        _internalFlags = EntityInternalFlags.None;
    }

    internal void Preinitialize(Scene scene) {
        Scene = scene;
        Depth = 0;
        ID = EntityID.Get();
    }

    public void Destroy() {
        if (IsDestroyed) return;

        InvokeDestroyCallbackRecursive(this);

        static void InvokeDestroyCallbackRecursive(Entity entity) {
            if (entity.IsDestroyed) return;

            entity.DoDestroy();

            foreach (var child in entity._children) {
                InvokeDestroyCallbackRecursive(child);
            }
        }
    }

    public bool IsDestroyed => _internalFlags.HasFlag(EntityInternalFlags.Destroyed);

    private void DoDestroy() {
        foreach (var component in _components) {
            if (component is IEntityLifecycleDestroy callback) {
                callback.OnDestroy();
            }
        }

        _internalFlags |= EntityInternalFlags.Destroyed;
        Depth = -1;
        Scene = null!;
        ID = 0;
        _children.Clear();
        _components.Clear();
    }
}