using Silk.NET.Windowing;

namespace RiptideEditorV2; 

partial class Application {
    private static RiptideServices _services = null!;
    
    private static void InitializeServices() {
        _services = new();
        
        _services.CreateService<IRenderingService, RenderingService, ContextOptions>(new() {
            Api = RenderingAPI.Direct3D12,
            OutputWindow = _window,
        });
        _services.CreateService<IInputService, SilkInputService, IView>(_window);
        _services.CreateService<ILoggingService, LoggingService>();
        
        Graphics.Initialize(_services);
    }
}