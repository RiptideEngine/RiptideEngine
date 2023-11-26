using Riptide.LowLevel.TextEngine;
using Riptide.UserInterface;

namespace RiptideEditorV2; 

public static partial class Application {
    private static BaseRenderingContext _renderContext = null!;
    private static InterfaceController _interface = null!;

    private static Font _font = null!;
    
    internal static void Initialize() {
        InitializeWindow();
    }

    private static void Init() {
        InitializeServices();
        RuntimeFoundation.Initialize(_services);
        
        FontEngine.Initialize();
        
        _renderContext = _services.GetRequiredService<IRenderingService>().Context;
        _renderContext.Logger = _services.GetRequiredService<ILoggingService>();
        
        ShaderCompilationPipeline.Initialize(_renderContext);

        _interface = new(Graphics.RenderingContext);
        _interface.Viewport = new(0, 0, _window.Size.X, _window.Size.Y);
        
        _font = Font.Import(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "times.ttf"), new(768, 768), 106, stackalloc CodepointRange[] {
            new(32, 127),
        })!;
        _font.Name = "Font";
    }
    private static void Update(double dt) {
        _interface.Update();
    }
    private static float _elapsedTime = 0;
    private static void Render(double dt) {
        _elapsedTime += (float)dt;
        
        var cmdList = _renderContext.Factory.CreateCommandList();
        
        cmdList.TranslateState(_renderContext.SwapchainCurrentRenderTarget.Resource, ResourceTranslateStates.RenderTarget);
        
        cmdList.SetRenderTarget(_renderContext.SwapchainCurrentRenderTarget.View, null);
        cmdList.ClearRenderTarget(_renderContext.SwapchainCurrentRenderTarget.View, new(0, 0.2f, 0.4f), ReadOnlySpan<Bound2DInt>.Empty);
        cmdList.SetScissorRect(new(0, 0, _window.Size.X, _window.Size.Y));
        cmdList.SetViewport(new(0, 0, _window.Size.X, _window.Size.Y));

        var renderingList = _interface.GetRenderingList();

        renderingList.PushScissorRect(new(0, 0, _window.Size.X, _window.Size.Y));
        {
            var center = new Circle(_window.Size.X / 2f, _window.Size.Y / 2f, 40);
            renderingList.DrawFilledCircle(center, Color32.Red, 16);

             for (int i = 0; i < 16; i++) {
                 renderingList.DrawFilledCircle(new(center.Position + Vector2.TransformNormal(new(200 + float.Sin(_elapsedTime * 5) * 80, 0), Matrix3x2.CreateRotation(float.Tau / 31 * i + _elapsedTime * float.Pi)), 40), Color32.Red, 16);
             }
        }
        renderingList.PopClipRect();
        
        _interface.EnqueueRenderingList(renderingList);
        
        _interface.Render(cmdList);
        
        cmdList.TranslateState(_renderContext.SwapchainCurrentRenderTarget.Resource, ResourceTranslateStates.Present);
        
        cmdList.Close();
        _renderContext.ExecuteCommandList(cmdList);
        _renderContext.WaitForGpuIdle();
        
        cmdList.DecrementReference();
        
        _renderContext.Present();
    }
    private static void Shutdown() {
        _font.DecrementReference();
        
        ShaderCompilationPipeline.Shutdown();
        FontEngine.Shutdown();
        RuntimeFoundation.Shutdown();
        
        _interface.Dispose();
        _services.RemoveAllServices();
    }
}