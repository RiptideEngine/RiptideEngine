namespace RiptideEditor.Assets;

partial class EditorResourceDatabase {
    public static ImportingResult LoadResource<T>(string path) where T : IResourceAsset {
        if (!_fileCatalogue.TryMapPathToGuid(path, out var guid)) return ImportingResult.FromError(ImportingError.UnmappedResourcePath);

        return _foundationDatabase.LoadResource(new ImportingLocation("file", guid), typeof(T), null);
    }

    public static ImportingResult LoadResource<T>(Guid guid) where T : IResourceAsset {
        if (!_fileCatalogue.Contains(guid)) return ImportingResult.FromError(ImportingError.UnmappedResourceGuid);

        return _foundationDatabase.LoadResource(new ImportingLocation("file", guid), typeof(T), null);
    }
}