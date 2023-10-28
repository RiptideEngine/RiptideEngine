namespace RiptideEditor.Assets;

internal sealed class EditorIdentifierCatalogue : IdentifierCatalogue {
    private readonly Dictionary<Guid, string> _guidToPath;
    private readonly Dictionary<string, Guid> _pathToGuid;

    public EditorIdentifierCatalogue() {
        _guidToPath = [];
        _pathToGuid = [];
    }
 
    public void AddCatalogue(Guid guid, string path) {
        _guidToPath.Add(guid, path);
        _pathToGuid.Add(path, guid);
    }

    public IEnumerable<KeyValuePair<Guid, string>> EnumerateGuidToPath() => _guidToPath;
    public IEnumerable<KeyValuePair<string, Guid>> EnumeratePathToGuid() => _pathToGuid;

    public override bool TryMapGuidToPath(Guid guid, [NotNullWhen(true)] out string? path) {
        return _guidToPath.TryGetValue(guid, out path);
    }

    public override bool TryMapPathToGuid(string path, out Guid guid) {
        return _pathToGuid.TryGetValue(path, out guid);
    }
}