using Silk.NET.Windowing;

namespace Riptide.FontRenderingExperiment;

internal class Runner {
    public static IWindow MainWindow { get; set; } = null!;

    static void Main(string[] args) {
        WindowOptions options = WindowOptions.Default with {
            API = GraphicsAPI.None with {
                Flags = ContextFlags.Debug,
            },
            WindowState = WindowState.Maximized,
        };

        MainWindow = Window.Create(options);

        MainWindow.Load += Lifecycle.Initialize;
        MainWindow.Closing += Lifecycle.Shutdown;

        MainWindow.Update += Lifecycle.Update;
        MainWindow.Render += Lifecycle.Render;

        MainWindow.Run();

        MainWindow.Dispose(); MainWindow = null!;
    }
}