namespace RiptideEditor.Assets;

internal sealed class FileProtocolProvider : ProtocolProvider {
    public override ResourceStreams ProvideStream(string path, Type resourceType) {
        path = Path.Combine(EditorApplication.ProjectPath, path);

        if (!File.Exists(path)) return default;

        var optionsFile = Path.Join(Path.GetFileNameWithoutExtension(path.AsSpan()), ".options", Path.GetExtension(path.AsSpan()));

        return new ResourceStreams(File.OpenRead(path), File.Exists(optionsFile) ? File.OpenRead(optionsFile) : null);
    }
}

// TODO: Add URL provider?