namespace RiptideEditor;

partial class EditorApplication {
    public static IWindow MainWindow { get; private set; } = null!;

    private static void CreateMainWindow() {
        if (MainWindow != null) throw new InvalidOperationException("Main window has already been created.");

        WindowOptions options = WindowOptions.Default with {
            API = GraphicsAPI.None with {
#if DEBUG
                Flags = ContextFlags.Debug,
#endif
            },
            WindowState = WindowState.Maximized,
        };
        MainWindow = Window.Create(options);

        MainWindow.Update += Update;
        MainWindow.Render += Render;

        MainWindow.Closing += Shutdown;
        MainWindow.Resize += Resize;
    }
}