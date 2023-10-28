namespace RiptideEditor.Assets;

public static partial class EditorResourceDatabase {
    private static RiptideServices _services = null!;
    private static ResourceDatabase _foundationDatabase = null!;

    private const string GuidsFileName = $"resourceids{AssetFileExtensions.ResourceIDs}";

    public static string AssetDirectory { get; private set; } = string.Empty;

    private static EditorIdentifierCatalogue _fileCatalogue = null!;

    internal static void Initialize(RiptideServices services, string projectPath) {
        Debug.Assert(_foundationDatabase == null);

        _services = services;
        _foundationDatabase = services.CreateService<IResourceDatabase, ResourceDatabase>();

        AssetDirectory = Path.Combine(projectPath, "Assets");

        _fileCatalogue = new();

        _foundationDatabase.RegisterProtocolProvider("file", new FileProtocolProvider());
        _foundationDatabase.RegisterIdentifierCatalogue("file", _fileCatalogue);
        _foundationDatabase.RegisterResourceImporter(new SceneImporter());

        InitializeGuids();

        Console.WriteLine(string.Join('\n', _fileCatalogue.EnumerateGuidToPath().Select(x => x.Key + ": " + x.Value)));
    }

    internal static void Shutdown() {
        bool removal = _services.RemoveService<IResourceDatabase>();
        Debug.Assert(removal);

        _services = null!;
        _foundationDatabase = null!;
    }
}