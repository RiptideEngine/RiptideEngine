using RiptideEditor.Application;

namespace RiptideEditor;

public static unsafe partial class EditorApplication {
    private static ImGuiController _imguiController = null!;

    public static Logger Logger { get; private set; } = null!;

    internal static ImGuiController ImguiController => _imguiController;

    private static BaseRenderingContext _renderingContext = null!;
    public static BaseRenderingContext RenderingContext => _renderingContext;

    public static string ProjectPath { get; private set; } = string.Empty;

    public static Timeline Time { get; private set; } = null!;

    private static ApplicationState? _queueingState;
    private static ApplicationState _currentState = null!;

    internal static void Initialize() {
        // if (!Path.IsPathFullyQualified(projectPath)) throw new ArgumentException("Project path must be fully qualified.");

        Logger = new(100);
        Logger.OnLogAdded += (record) => {
            var cc = Console.ForegroundColor;

            Console.ForegroundColor = record.Type switch {
                LoggingType.Info => ConsoleColor.Gray,
                LoggingType.Warning => ConsoleColor.Yellow,
                _ => ConsoleColor.DarkRed
            };

            Console.WriteLine(record.Message);

            Console.ForegroundColor = cc;
        };

        CreateMainWindow();

        MainWindow.Load += InitializeImpl;

        MainWindow.Run();
    }

    private static void InitializeImpl() {
        Time = new();
        Services = new();

        CreateSystemServices(Services);
        RuntimeFoundation.Initialize(Services);

        // EditorResourceDatabase.Initialize(Services, ProjectPath);
        EditorScene.Initialize(Services);
        GameRuntimeContext.Initialize(Services);

        _imguiController = new(Services, new(MainWindow.Size.X, MainWindow.Size.Y));
        ImGui.StyleColorsDark();

        // CreateGameRuntimeServices(Services);

        // RiptideSerialization.Initialize(Services);

        // Graphics.RenderingPipeline = new TestRenderingPipeline();

        //Toolbar.Initialize();

        //EditorWindows.GetOrAddWindowInstance<SceneViewWindow>();
        //EditorWindows.GetOrAddWindowInstance<GameViewWindow>();
        //EditorWindows.GetOrAddWindowInstance<InspectorWindow>();
        //EditorWindows.GetOrAddWindowInstance<HierarchyWindow>();
        //EditorWindows.GetOrAddWindowInstance<ConsoleWindow>();
        //EditorWindows.GetOrAddWindowInstance<AssetBrowserWindow>();

        _currentState = new ProjectOpenState();
        _currentState.Begin();
    }

    internal static void Update(double deltaTime) {
        _imguiController.Update((float)deltaTime);

        _currentState.Update();

        _currentState.RenderGUI();

        if (_queueingState != null) {
            _currentState.End();
            _currentState = _queueingState;
            _currentState.Begin();
        }

        //Shortcuts.Execute();

        //if (PlaymodeProcedure.IsInPlaymode) {
        //    if (!PlaymodeProcedure.IsPaused) {
        //        foreach (var scene in EditorScene.EnumerateScenes()) {
        //            scene.Tick();
        //        }
        //    }
        //}
    }

    internal static void Render(double deltaTime) {
        var cmdList = _renderingContext.Factory.CreateCommandList();

        (var swapchainTexture, var swapchainRTV) = _renderingContext.SwapchainCurrentRenderTarget;

        // Render the GUI
        cmdList.TranslateResourceStates([
            new(swapchainTexture, ResourceStates.Present, ResourceStates.RenderTarget),
        ]);

        cmdList.SetRenderTarget(swapchainRTV);
        cmdList.ClearRenderTarget(swapchainRTV, RiptideMathematics.Color.Black);

        _imguiController.Render(cmdList);

        cmdList.TranslateResourceStates([
            new(swapchainTexture, ResourceStates.RenderTarget, ResourceStates.Present),
        ]);

        cmdList.Close();
        _renderingContext.ExecuteCommandList(cmdList);
        _renderingContext.WaitForGpuIdle();

        cmdList.DecrementReference();

        _renderingContext.Present();
    }

    internal static void Resize(Silk.NET.Maths.Vector2D<int> size) {
        if (size.X == 0 || size.Y == 0) return;

        _renderingContext.ResizeSwapchain((uint)size.X, (uint)size.Y);

        _currentState.Resize(size.X, size.Y);
    }

    internal static T SwitchState<T>() where T : ApplicationState, new() {
        _queueingState = new T();
        return Unsafe.As<T>(_queueingState);
    }

    public static void Shutdown() {
        // Toolbar.Shutdown();

        _currentState.Shutdown();

        // EditorWindows.RemoveAllWindows();
        _imguiController?.Dispose();

        // EditorResourceDatabase.Shutdown();
        RuntimeFoundation.Shutdown();

        Services.RemoveAllServices();
    }
}