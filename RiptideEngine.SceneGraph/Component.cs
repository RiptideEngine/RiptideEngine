namespace RiptideEngine.SceneGraph;

public abstract class Component : IInstantiatable {
    [JsonIgnore] public Entity Entity { get; private set; } = null!;
    [JsonIgnore] public uint ID { get; private set; }

    internal void Preinitialize(Entity entity) {
        if (Entity != null) throw new InvalidOperationException("The component has already been assigned to an Entity.");

        PreinitializeUnchecked(entity);
    }

    internal void PreinitializeUnchecked(Entity entity) {
        Entity = entity;
        ID = ComponentID.Get();
    }

    public virtual bool CanInstantiate<T>() => typeof(T) == typeof(Entity) || typeof(T) == GetType();
    public virtual bool CanInstantiate(Type type) => type == typeof(Entity) || type == GetType();

    public virtual bool TryInstantiate<T>([NotNullWhen(true)] out T? output) {
        if (typeof(T) == typeof(Entity)) {
            output = (T)(object)InstantiateAsEntity();
            return true;
        }
        if (typeof(T) == GetType()) {
            output = (T)(object)InstantiateSelf();
            return true;
        }

        output = default;
        return false;
    }

    public virtual bool TryInstantiate(Type type, [NotNullWhen(true)] out object? output) {
        if (type == typeof(Entity)) {
            output = InstantiateAsEntity();
            return true;
        }
        if (type == GetType()) {
            output = InstantiateSelf();
            return true;
        }

        output = null;
        return false;
    }

    private Entity InstantiateAsEntity() {
        var entity = new Entity() {
            Name = Entity?.Name,
        };

        entity.AddComponentByInstantiate(this);

        return entity;
    }

    protected virtual Component InstantiateSelf() {
        var type = GetType();
        return (Component)JsonSerializer.Deserialize(JsonSerializer.Serialize(this, type), type)!;
    }

    public virtual void Initialize() { }
    public virtual void Destroy() { }
}