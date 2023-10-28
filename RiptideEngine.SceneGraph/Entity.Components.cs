namespace RiptideEngine.SceneGraph;

partial class Entity {
    private readonly List<Component> _components;

    public int ComponentsCount => _components.Count;

    internal void AddDeserializedComponent(Component component) {
        ArgumentNullException.ThrowIfNull(component);

        component.Preinitialize(this);
        _components.Add(component);

        component.Initialize();
    }

    internal void AddComponentByInstantiate(Component source) {
        ArgumentNullException.ThrowIfNull(source);

        if (source.TryInstantiate(source.GetType(), out var instantiatedObj) && instantiatedObj is Component instantiatedComponent) {
            instantiatedComponent.Preinitialize(this);
            _components.Add(instantiatedComponent);

            instantiatedComponent.Initialize();
        }
    }

    public Component AddComponent(Type type) {
        if (!type.IsSubclassOf(typeof(Component))) throw new ArgumentException($"Type '{type.FullName}' does not inherit from {typeof(Component).FullName}, thus it is not a valid component type.");

        var component = Unsafe.As<Component>(Activator.CreateInstance(type, true)!);
        component.PreinitializeUnchecked(this);
        _components.Add(component);

        component.Initialize();

        return component;
    }

    public T AddComponent<T>() where T : Component, new() {
        var component = new T();
        component.PreinitializeUnchecked(this);
        _components.Add(component);

        component.Initialize();

        return component;
    }

    public bool TryGetComponent<T>([NotNullWhen(true)] out T? output) where T : Component {
        foreach (var component in _components) {
            if (component is T t) {
                output = t;
                return true;
            }
        }

        output = null;
        return false;
    }

    public T? GetComponent<T>() where T : Component {
        TryGetComponent(out T? component);
        return component;
    }

    public T? RemoveComponent<T>() where T : Component {
        for (int i = 0; i < _components.Count; i++) {
            if (_components[i] is T t) {
                _components.RemoveAt(i);
                t.Destroy();
                return t;
            }
        }

        return null;
    }

    public IEnumerable<Component> EnumerateComponents() => _components;
    public IEnumerable<T> EnumerateComponents<T>() => _components.OfType<T>();
}