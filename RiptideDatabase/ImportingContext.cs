namespace RiptideDatabase;

public readonly record struct ResourceDependency(string Key, ImportingLocation Location, Type ResourceType);

public sealed class ImportingContext {
    private readonly Stack<Dictionary<string, DependencyProfile>> _dependencies = [];

    public void PushDependencyScope() {
        _dependencies.Push([]);
    }

    public void AddResourceDependency(ResourceDependency dependency) {
        if (!_dependencies.TryPeek(out var dict)) return;

        dict[dependency.Key] = dependency;
    }

    public IDictionary<string, DependencyProfile> PopDependencyScope() {
        return _dependencies.Pop();
    }

    public readonly record struct DependencyProfile(ImportingLocation Location, Type ResourceType) {
        public static implicit operator DependencyProfile(ResourceDependency dependency) {
            return new(dependency.Location, dependency.ResourceType);
        }
    }
}