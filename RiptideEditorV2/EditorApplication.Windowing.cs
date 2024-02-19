using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace RiptideEditorV2; 

partial class EditorApplication {
    private static IWindow _window = null!;

    public static Vector2UInt WindowSize => Unsafe.BitCast<Vector2D<int>, Vector2UInt>(_window.Size);
    
    private static void CreateWindow() {
        _window = Window.Create(WindowOptions.Default with {
            API = new() {
                API = ContextAPI.None,
#if DEBUG
                Flags = ContextFlags.Debug,
#endif
            },
            WindowState = WindowState.Maximized,
        });

        _window.Load += Init;
        _window.Update += Update;
        _window.Render += Render;
        _window.Closing += Shutdown;
        _window.Resize += Resize;

        _window.Run();
        _window.Dispose();
    }
}