using Silk.NET.Maths;

namespace RiptideEditor;

partial class EditorApplication {
    internal static RiptideServices Services { get; private set; } = null!;

    private static void CreateSystemServices(RiptideServices services) {
        services.CreateService<IInputService, InputService, IWindow>(MainWindow);
        _renderingContext = services.CreateService<IRenderingService, RenderingService, ContextOptions>(new(RenderingAPI.Direct3D12, MainWindow, typeof(Vector2D<int>))).Context;
    }

    private static void CreateGameRuntimeServices(RiptideServices services) {
        services.CreateService<IAssimpLibrary, AssimpLibraryService>();
        services.CreateService<IComponentDatabase, ComponentDatabaseService>();
    }
}