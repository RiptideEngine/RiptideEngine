using Silk.NET.Windowing;

namespace RiptideEditorV2; 

partial class EditorApplication {
    private static RiptideServices _services = null!;
    public static RiptideServices Services => _services;
    
    private static void InitializeServices() {
        _services = new();
        
        _services.CreateService<IRenderingService, RenderingService, ContextOptions>(new() {
            Api = RenderingAPI.Direct3D12,
            OutputWindow = _window,
            Logger = _services.CreateService<ILoggingService, LoggingService>(),
            SwapchainFormat = GraphicsFormat.R8G8B8A8UNorm,
        });
        _services.CreateService<IInputService, SilkInputService, IView>(_window);
    }
}