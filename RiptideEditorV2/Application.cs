using Riptide.LowLevel.TextEngine;

namespace RiptideEditorV2; 

public static partial class Application {
    private static RenderingContext _renderContext = null!;

    private static Font _font = null!;
    
    internal static void Initialize() {
        InitializeWindow();
    }

    private static void Init() {
        InitializeServices();
        _renderContext = _services.GetRequiredService<IRenderingService>().Context;
        
        RuntimeFoundation.Initialize(_services);
        FontEngine.Initialize();
        ShaderCompilationPipeline.Initialize(_renderContext);
        
        _renderContext.Logger = _services.GetRequiredService<ILoggingService>();

        _font = Font.Import(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "times.ttf"), new(768, 768), 106, stackalloc CodepointRange[] {
            new(32, 127),
        })!;
        _font.Name = "Font";
    }
    private static void Update(double dt) {
    }
    private static void Render(double dt) {
        var cmdList = _renderContext.Factory.CreateGraphicsCommandList();
        
        cmdList.TranslateResourceState(_renderContext.SwapchainCurrentRenderTarget.Resource, ResourceTranslateStates.RenderTarget);
        
        cmdList.SetRenderTarget(_renderContext.SwapchainCurrentRenderTarget.View, null);
        cmdList.ClearRenderTarget(_renderContext.SwapchainCurrentRenderTarget.View, new(0, 0, 0, 1), ReadOnlySpan<Bound2DInt>.Empty);
        cmdList.SetScissorRect(new(0, 0, _window.Size.X, _window.Size.Y));
        cmdList.SetViewport(new(0, 0, _window.Size.X, _window.Size.Y, 0, 1));
        
        cmdList.TranslateResourceState(_renderContext.SwapchainCurrentRenderTarget.Resource, ResourceTranslateStates.Present);
        
        cmdList.Close();
        _renderContext.ExecuteCommandList(cmdList);
        cmdList.DecrementReference();
        
        _renderContext.Present();
    }
    private static void Shutdown() {
        _font.DecrementReference();
        
        ShaderCompilationPipeline.Shutdown();
        FontEngine.Shutdown();
        RuntimeFoundation.Shutdown();
        
        _services.RemoveAllServices();
    }
}