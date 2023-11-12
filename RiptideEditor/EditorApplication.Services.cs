namespace RiptideEditor;

partial class EditorApplication {
    internal static RiptideServices Services { get; private set; } = null!;

    private static void CreateSystemServices(RiptideServices services) {
        var logger = services.CreateService<ILoggingService, LoggingService>();
        services.CreateService<IInputService, InputService, IWindow>(MainWindow);
        _renderingContext = services.CreateService<IRenderingService, RenderingService, ContextOptions>(new(RenderingAPI.Direct3D12, MainWindow)).Context;
        _renderingContext.Logger = logger;
    }

    private static void CreateGameRuntimeServices(RiptideServices services) {
        services.CreateService<IAssimpLibrary, AssimpLibraryService>();
        services.CreateService<IComponentDatabase, ComponentDatabaseService>();
    }
}