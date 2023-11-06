namespace RiptideEditor.Assets;

internal sealed class FileProtocolProvider : ProtocolProvider {
    public override ResourceStreams ProvideStream(string path, Type resourceType) {
        path = Path.Combine(EditorApplication.ProjectPath, path);

        if (!File.Exists(path)) return default;

        var optionsFile = Path.Join(path, AssetFileExtensions.ImporterOptions);

        return new ResourceStreams(File.OpenRead(path), File.Exists(optionsFile) ? File.OpenRead(optionsFile) : null);
    }
}

// TODO: Add URL provider?