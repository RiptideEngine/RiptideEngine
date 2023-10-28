namespace RiptideDatabase;

public readonly record struct ResourceDependency(string Key, ImportingLocation Location, Type ResourceType);

public sealed class ImportingContext(IResourceDatabase database) {
    private readonly IResourceDatabase _database = database;
    
    private readonly Stack<DependencyScope> _dependencies = [];
    private readonly HashSet<DependencyProfile> _dependencyDeduplications = [];

    public void PushDependencyScope() {
        _dependencies.Push(new DependencyScope());
    }

    public void AddResourceDependency(ResourceDependency dependency) {
        if (!_dependencyDeduplications.Add(dependency)) return;

        var dependencies = _dependencies.Peek().Dependencies;
        ref var refdep = ref CollectionsMarshal.GetValueRefOrAddDefault(dependencies, dependency.Key, out bool exists);
        if (exists) return;

        refdep = dependency;
    }

    public IDictionary<string, DependencyProfile> PopDependencyScope() {
        var dependencies = _dependencies.Pop().Dependencies;
        return dependencies;
    }

    public readonly record struct DependencyProfile(ImportingLocation Location, Type ResourceType) {
        public static implicit operator DependencyProfile(ResourceDependency dependency) {
            return new(dependency.Location, dependency.ResourceType);
        }
    }
    private sealed class DependencyScope {
        public readonly Dictionary<string, DependencyProfile> Dependencies;
    
        public DependencyScope() {
            Dependencies = [];
        }
    }
}