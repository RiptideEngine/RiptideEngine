namespace RiptideEditor.Assets;

partial class EditorResourceDatabase {
    public static bool TryConvertResourceIDToPath(Guid rid, [NotNullWhen(true)] out string? path) => _fileCatalogue.TryMapGuidToPath(rid, out path);
    public static bool TryConvertPathToResourceID(string path, out Guid rid) => _fileCatalogue.TryMapPathToGuid(path, out rid);

    public static string? ConvertResourceIDToPath(Guid guid) => _fileCatalogue.TryMapGuidToPath(guid, out var path) ? path : null;
    public static Guid ConvertPathToResourceID(string path) => _fileCatalogue.TryMapPathToGuid(path, out var guid) ? guid : default;

    private static void InitializeGuids() {
        foreach (var dir in Directory.EnumerateDirectories(AssetDirectory, "*", SearchOption.AllDirectories).Prepend(AssetDirectory)) {
            var ridFile = Path.Combine(dir, GuidsFileName);

            if (File.Exists(ridFile)) {
                using var fs = new FileStream(ridFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                Dictionary<string, Guid> deserialized;
                try {
                    deserialized = JsonSerializer.Deserialize<Dictionary<string, Guid>>(fs)!;
                } catch (JsonException) {
                    EditorApplication.Logger.Log(LoggingType.Error, $"Failed to deserialize Guids file at path '{ridFile}'. New Guids will be generated, but all old resource references will be lost.");

                    fs.SetLength(0);

                    using var writer = new Utf8JsonWriter(fs);

                    RegenerateGuids(writer, dir);

                    continue;
                }

                var track = DictionaryPool<string, Guid>.Shared.Get();
                bool needRewrite = false;

                foreach (var file in EnumerateAssetFileSystems(AssetDirectory, SearchOption.TopDirectoryOnly)) {
                    var filename = Path.GetFileName(file);

                    if (deserialized.TryGetValue(filename, out var rid)) {
                        track.Add(filename, rid);

                        _fileCatalogue.AddCatalogue(rid, TruncateResourcePath(file)!);

                        continue;
                    }

                    needRewrite = true;
                    rid = Guid.NewGuid();
                    track.Add(filename, rid);

                    _fileCatalogue.AddCatalogue(rid, TruncateResourcePath(file)!);
                }

                if (needRewrite) {
                    fs.SetLength(0);
                    JsonSerializer.Serialize(fs, track);
                }

                DictionaryPool<string, Guid>.Shared.Return(track);
            } else {
                using var fs = new FileStream(ridFile, FileMode.Create, FileAccess.Write, FileShare.None);
                using var writer = new Utf8JsonWriter(fs);

                RegenerateGuids(writer, dir);
            }
        }

        static void RegenerateGuids(Utf8JsonWriter writer, string directory) {
            writer.WriteStartObject();

            foreach (var file in EnumerateAssetFileSystems(directory, SearchOption.TopDirectoryOnly)) {
                writer.WritePropertyName(Path.GetFileName(file.AsSpan()));

                var rid = Guid.NewGuid();
                JsonSerializer.Serialize(writer, rid);

                _fileCatalogue.AddCatalogue(rid, TruncateResourcePath(file)!);
            }

            writer.WriteEndObject();
        }
    }

    public static bool DeleteAsset(string relativePath) {
        if (!_fileCatalogue.Contains(relativePath)) return false;

        var fullPath = Path.Combine(EditorApplication.ProjectPath, relativePath);

        if (!File.Exists(fullPath)) return false;

        File.Delete(fullPath);

        var ridFile = Path.Combine(fullPath, "..", AssetFileExtensions.ResourceIDs);
        Debug.Assert(File.Exists(ridFile));

        using var fs = new FileStream(ridFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Dictionary<string, Guid> rids = JsonSerializer.Deserialize<Dictionary<string, Guid>>(fs)!;

        bool removal = rids.Remove(Path.GetFileName(fullPath));
        Debug.Assert(removal);

        fs.SetLength(0);
        JsonSerializer.Serialize(fs, rids);

        return true;
    }

    public static bool MoveAsset(string source, string destination) {
        return false;
    }
}
