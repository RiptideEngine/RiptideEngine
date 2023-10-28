namespace RiptideDatabase;

public abstract class IdentifierCatalogue {
    public virtual bool Contains(string path) => TryMapPathToGuid(path, out _);
    public virtual bool Contains(Guid guid) => TryMapGuidToPath(guid, out _);

    public abstract bool TryMapGuidToPath(Guid guid, [NotNullWhen(true)] out string? path);
    public abstract bool TryMapPathToGuid(string path, out Guid guid);
}