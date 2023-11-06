namespace RiptideEditor;

public static unsafe partial class EditorApplication {
    private static ImGuiController _imguiController = null!;

    public static TimeTracker Time { get; private set; } = null!;

    public static Logger Logger { get; private set; } = null!;

    internal static ImGuiController ImguiController => _imguiController;

    private static BaseRenderingContext _renderingContext = null!;
    public static BaseRenderingContext RenderingContext => _renderingContext;

    public static string ProjectPath { get; private set; } = string.Empty;

    internal static void Initialize(string projectPath) {
        if (!Path.IsPathFullyQualified(projectPath)) throw new ArgumentException("Project path must be fully qualified.");
        
        Logger = new(100);
        Logger.OnLogAdded += (record) => {
            var cc = Console.ForegroundColor;

            switch (record.Type) {
                case LoggingType.Info: Console.ForegroundColor = ConsoleColor.Gray; break;
                case LoggingType.Warning: Console.ForegroundColor = ConsoleColor.Yellow; break;
                case LoggingType.Error or _: Console.ForegroundColor = ConsoleColor.DarkRed; break;
            }

            Console.WriteLine(record.Message);

            Console.ForegroundColor = cc;
        };

        //AppDomain.CurrentDomain.AssemblyLoad += (sender, e) => {
        //    Logger.Log(LoggingType.Info, "Assembly loaded: " + e.LoadedAssembly.FullName);
        //};

        ProjectPath = projectPath;

        CreateMainWindow();

        MainWindow.Load += InitializeImpl;

        MainWindow.Run();
    }

    private static void InitializeImpl() {
        Time = new();
        Services = new();

        CreateSystemServices(Services);

        EditorResourceDatabase.Initialize(Services, ProjectPath);
        EditorScene.Initialize(Services);
        GameRuntimeContext.Initialize(Services);

        CreateGameRuntimeServices(Services);
        RuntimeFoundation.Initialize(Services);

        // RiptideSerialization.Initialize(Services);

        Graphics.RenderingPipeline = new TestRenderingPipeline();

        _imguiController = new(Services, new(MainWindow.Size.X, MainWindow.Size.Y), Time);

        ImGui.StyleColorsDark();

        Toolbar.Initialize();

        EditorWindows.GetOrAddWindowInstance<SceneViewWindow>();
        EditorWindows.GetOrAddWindowInstance<GameViewWindow>();
        EditorWindows.GetOrAddWindowInstance<InspectorWindow>();
        EditorWindows.GetOrAddWindowInstance<HierarchyWindow>();
        EditorWindows.GetOrAddWindowInstance<ConsoleWindow>();
        EditorWindows.GetOrAddWindowInstance<AssetBrowserWindow>();
    }

    internal static void Update(double deltaTime) {
        Time.Update(deltaTime);
        _imguiController.Update();

        Shortcuts.Execute();

        if (PlaymodeProcedure.IsInPlaymode) {
            if (!PlaymodeProcedure.IsPaused) {
                foreach (var scene in EditorScene.EnumerateScenes()) {
                    scene.Tick();
                }
            }
        }

        var mainViewport = ImGui.GetMainViewport();

        MenuBar.Render();

        Toolbar.Render();

        ImGui.SetNextWindowPos(mainViewport.Pos + new Vector2(0, ImGui.GetFrameHeight() + Toolbar.ToolbarHeight));
        ImGui.SetNextWindowSize(new(mainViewport.Size.X, mainViewport.Size.Y - ImGui.GetFrameHeight() - Toolbar.ToolbarHeight));

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin("Main Workspace", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        ImGui.PopStyleVar(3);
        {
            ImGui.DockSpace(ImGui.GetID("Main Dockspace"), ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin(), ImGuiDockNodeFlags.PassthruCentralNode);

            ImGui.ShowDemoWindow();
            EditorWindows.RenderWindows();
        }
        ImGui.End();
    }

    internal static void Render(double deltaTime) {
        var cmdList = _renderingContext.Factory.CreateCommandList();

        (var swapchainTexture, var swapchainRTV) = _renderingContext.SwapchainCurrentRenderTarget;
        (var swapchainDepth, var swapchainDSV) = _renderingContext.Depth;

        // Render the GUI
        cmdList.TranslateResourceStates([
            new(swapchainTexture, ResourceStates.Present, ResourceStates.RenderTarget),
            new(swapchainDepth, ResourceStates.DepthRead, ResourceStates.DepthWrite),
        ]);

        cmdList.SetRenderTarget(swapchainRTV, swapchainDSV);
        cmdList.ClearRenderTarget(swapchainRTV, RiptideMathematics.Color.Black);
        cmdList.ClearDepthTexture(swapchainDSV, DepthClearFlags.All, 1, 0);

        _imguiController.Render(cmdList);

        cmdList.TranslateResourceStates([
            new(swapchainTexture, ResourceStates.RenderTarget, ResourceStates.Present),
            new(swapchainDepth, ResourceStates.DepthWrite, ResourceStates.DepthRead),
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

        _imguiController.SetDisplaySize(new(size.X, size.Y));
    }

    public static void Shutdown() {
        Toolbar.Shutdown();

        EditorWindows.RemoveAllWindows();
        _imguiController?.Dispose();

        EditorResourceDatabase.Shutdown();
        RuntimeFoundation.Shutdown();

        Services.RemoveAllServices();
    }
}