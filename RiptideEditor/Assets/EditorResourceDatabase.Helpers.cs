namespace RiptideEditor.Assets;

partial class EditorResourceDatabase {
    public static IEnumerable<string> EnumerateAssetFileSystems(string directory, SearchOption searchOption) {
        return Directory.EnumerateFileSystemEntries(directory, "*.*", searchOption).Where(ExtensionFilter);

        static bool ExtensionFilter(string path) {
            return Path.HasExtension(path) && Path.GetExtension(path.AsSpan()) is not AssetFileExtensions.ResourceIDs;
        }
    }

    /// <summary>
    /// Convert full resource path into path that relative to project path.
    /// </summary>
    /// <param name="resourceFullPath">Full path of resource.</param>
    /// <returns>Path relative to project path, or null <paramref name="resourceFullPath"/> doesn't start with project path. See <see cref="EditorApplication.ProjectPath"/> for extra information.</returns>
    public static string? TruncateResourcePath(string resourceFullPath) {
        return resourceFullPath.StartsWith(EditorApplication.ProjectPath) ? resourceFullPath.AsSpan()[EditorApplication.ProjectPath.Length..].Trim(Path.DirectorySeparatorChar).ToString() : null;
    }
}