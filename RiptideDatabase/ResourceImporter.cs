namespace RiptideDatabase;

public abstract class ResourceImporter {
    protected ResourceStreams _streams;
    protected ImportingLocation _importingLocation;

    public abstract bool CanImport(ImportingLocation location, Type resourceType);
    public virtual void Initialize(ResourceStreams stream, ImportingLocation location) {
        _streams = stream;
        _importingLocation = location;
    }
    public abstract void GetDependencies(ImportingContext context, ref object? userData);
    public abstract ImportingResult PartiallyLoadResource(object? userData);
    public virtual void PatchDependencies(object resource, IDictionary<string, object?> dependencies, object? userData) { }
}