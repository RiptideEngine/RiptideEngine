namespace RiptideDatabase;

public abstract class ResourceImporter {
    public abstract bool CanImport(ImportingLocation location, Type resourceType);

    public abstract ImportingResult RawImport(ResourceStreams streams);
    public virtual void GetDependencies(ImportingContext context, object rawObject) { }
    public abstract ImportingResult ImportPartially(object rawObject);
    public virtual void PatchDependencies(object rawObject, object resourceObject, IDictionary<string, object?> dependencies) { }
}