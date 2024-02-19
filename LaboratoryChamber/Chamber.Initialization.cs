using Silk.NET.Windowing;

namespace Riptide.Laboratory;

partial class Chamber {
    public static T Initialize<T>() where T : Chamber, new() {
        WindowOptions options = WindowOptions.Default with {
            API = GraphicsAPI.None with {
                Flags = ContextFlags.Debug,
            },
            WindowState = WindowState.Maximized,
        };

        var window = Window.Create(options);
        var chamber = new T {
            MainWindow = window,
        };

        window.Load += chamber.Initialize;
        window.Closing += chamber.Shutdown;

        window.Update += chamber.Update;
        window.Render += chamber.Render;

        window.Resize += chamber.Resize;

        return new() {
            MainWindow = window,
        };
    }
}