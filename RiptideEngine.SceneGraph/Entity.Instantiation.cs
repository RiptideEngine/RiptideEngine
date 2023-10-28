
namespace RiptideEngine.SceneGraph;

partial class Entity : IInstantiatable {
    public bool CanInstantiate<T>() => typeof(T) == typeof(Entity);
    public bool CanInstantiate(Type outputType) => outputType == typeof(Entity);

    public bool TryInstantiate<T>([NotNullWhen(true)] out T? output) {
        if (typeof(T) != typeof(Entity)) {
            output = default;
            return false;
        }

        output = (T)(object)Instantiate();
        return true;
    }

    public bool TryInstantiate(Type outputType, [NotNullWhen(true)] out object? output) {
        if (outputType != typeof(Entity)) {
            output = default;
            return false;
        }

        output = Instantiate();
        return true;
    }

    private Entity Instantiate() {
        var entity = new Entity() {
            Name = Name,
        };

        entity.LocalPosition = entity.GlobalPosition = GlobalPosition;
        entity.LocalRotation = entity.GlobalRotation = GlobalRotation;
        entity.LocalScale = entity.GlobalScale = GlobalScale;

        foreach (var component in _components) {
            entity.AddComponentByInstantiate(component);
        }

        return entity;
    }
}