namespace RiptideEditor;

public static class EditorScene {
    internal static JsonSerializerOptions SceneSerializationOptions { get; private set; } = null!;

    private static SceneGraphService _sgservice = null!;

    private static readonly Dictionary<Scene, string> _scenePaths = [];
    private static readonly HashSet<Scene> _dirtyScenes = [];
    private static readonly List<Scene> _createdScenes = [];

    internal static void Initialize(RiptideServices services) {
        if (_sgservice != null) throw new InvalidOperationException($"{nameof(EditorScene)} is already initialized.");

        _sgservice = services.CreateService<ISceneGraphService, SceneGraphService>();

        SceneSerializationOptions = new() {
            WriteIndented = false,
            IncludeFields = true,
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        };
        SceneSerializationOptions.Converters.Add(new EditorSceneConverterFactory());

        ProjectSaving.SaveOperation += DoSaveOperation;
    }

    public static bool OpenScene(string assetPath) {
        if (Path.GetExtension([.. assetPath]) is not AssetFileExtensions.Scene) return false;
        if (_scenePaths.ContainsValue(assetPath)) return false;

        var result = EditorResourceDatabase.LoadResource<Scene>(assetPath);
        
        if (result.HasError) {
            EditorApplication.Logger.Log(LoggingType.Error, $"Failed to open scene at path '{assetPath}'. Error: '{result.Error}'.");
            return false;
        }

        Scene? instance = ((Scene)result.Result!).Instantiate<Scene>();
        if (instance == null) return false;

        _scenePaths.Add(instance, assetPath);
        _sgservice.Context.AddScene(instance);
        return true;
    }

    public static bool CloseScene(Scene scene) {
        if (_sgservice.Context.RemoveScene(scene)) {
            bool remove = _scenePaths.Remove(scene);
            Debug.Assert(remove);

            _dirtyScenes.Remove(scene);

            return true;
        }

        return false;
    }

    //public static bool OpenEntityTemplatePreviewScene(string assetPath) {
    //    if (Path.GetExtension(assetPath.AsSpan()) is not AssetFileExtensions.EntityTemplate) return false;

    //    var scene = _sgservice.TemplatePreviewContext.CreateScene();
    //    scene.Name = name;

    //    _sgservice.SwitchToTemplatePreviewContext();

    //    return scene;
    //}

    public static void SaveScene(Scene scene) {
        if (!_dirtyScenes.Contains(scene)) return;

        var assetPath = _scenePaths[scene];

        Debug.Assert(!string.IsNullOrEmpty(assetPath));

        using var fs = new FileStream(Path.Combine(EditorApplication.ProjectPath, assetPath), FileMode.Open, FileAccess.Write, FileShare.None);
        fs.SetLength(0);

        using var writer = new Utf8JsonWriter(fs, new JsonWriterOptions() {
            Indented = false,
        });

        JsonSerializer.Serialize(writer, scene, SceneSerializationOptions);
    }

    public static Scene CreateScene() {
        var scene = _sgservice.Context.CreateScene();
        _createdScenes.Add(scene);

        return scene;
    }

    public static void TestSerializeScene(Scene scene) {
        Console.WriteLine(JsonSerializer.Serialize(scene, new JsonSerializerOptions(SceneSerializationOptions) {
            WriteIndented = true,
        }));
    }

    public static bool MarkSceneDirty(Scene scene) {
        if (!_sgservice.Context.ContainsScene(scene)) return false;

        _dirtyScenes.Add(scene);
        return true;
    }

    public static bool IsSceneDirty(Scene scene) => _dirtyScenes.Contains(scene);
    public static IEnumerable<Scene> EnumerateScenes() => _sgservice.Context.EnumerateScenes();
    public static IEnumerable<Scene> EnumerateCreatedScenes() => _createdScenes;

    private static void DoSaveOperation() {
        foreach (var scene in _sgservice.Context.EnumerateScenes()) {
            SaveScene(scene);
        }

        _dirtyScenes.Clear();
    }
}