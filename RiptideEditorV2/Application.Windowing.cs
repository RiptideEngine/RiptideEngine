using Silk.NET.Windowing;

namespace RiptideEditorV2; 

partial class Application {
    private static IWindow _window = null!;
    
    private static void InitializeWindow() {
        _window = Window.Create(WindowOptions.Default with {
            API = new() {
                API = ContextAPI.None,
                Flags = ContextFlags.Debug,
            },
            WindowState = WindowState.Maximized,
        });

        _window.Load += Init;
        _window.Update += Update;
        _window.Render += Render;
        _window.Closing += Shutdown;

        _window.Run();
        _window.Dispose();
    }
}