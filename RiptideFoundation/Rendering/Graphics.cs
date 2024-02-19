using Riptide.ShaderCompilation;
using RiptideFoundation.Rendering;

namespace RiptideFoundation.Rendering;

public static class Graphics {
    public static RenderingContext RenderingContext { get; private set; } = null!;

    internal static ResourceSignatureStorage ResourceSignatureStorage { get; private set; } = null!;

    public static ShaderCompilationFactory CompilationFactory { get; private set; } = null!;

    public static Texture2D WhiteTexture { get; private set; } = null!;
    public static Texture2D BlackTexture { get; private set; } = null!;
    public static Texture2D RedTexture { get; private set; } = null!;
    public static Texture2D GreenTexture { get; private set; } = null!;
    public static Texture2D BlueTexture { get; private set; } = null!;

    public static void Initialize(RiptideServices services) {
        Debug.Assert(RenderingContext == null, "RenderingContext == null");
        
        RenderingContext = services.GetRequiredService<IRenderingService>().Context;
        ResourceSignatureStorage = new();
        CompilationFactory = new(RenderingContext.RenderingAPI);
        
        ShaderCompilationEngine.Initialize();

        WhiteTexture = new(1, 1, GraphicsFormat.R8G8B8A8UNorm) {
            Name = "White Texture",
        };

        BlackTexture = new(1, 1, GraphicsFormat.R8G8B8A8UNorm) {
            Name = "Black Texture",
        };
        
        RedTexture = new(1, 1, GraphicsFormat.R8G8B8A8UNorm) {
            Name = "Red Texture",
        };
        
        GreenTexture = new(1, 1, GraphicsFormat.R8G8B8A8UNorm) {
            Name = "Green Texture",
        };
        
        BlueTexture = new(1, 1, GraphicsFormat.R8G8B8A8UNorm) {
            Name = "Blue Texture",
        };

        var cmdList = RenderingContext.Factory.CreateCommandList();

        cmdList.UpdateTexture(WhiteTexture.UnderlyingTexture, 0, MemoryMarshal.AsBytes([Color32.White]));
        cmdList.UpdateTexture(BlackTexture.UnderlyingTexture, 0, MemoryMarshal.AsBytes([Color32.Black]));
        cmdList.UpdateTexture(RedTexture.UnderlyingTexture, 0, MemoryMarshal.AsBytes([Color32.Red]));
        cmdList.UpdateTexture(GreenTexture.UnderlyingTexture, 0, MemoryMarshal.AsBytes([Color32.Green]));
        cmdList.UpdateTexture(BlueTexture.UnderlyingTexture, 0, MemoryMarshal.AsBytes([Color32.Blue]));
        
        cmdList.Close();
        RenderingContext.Synchronizer.WaitCpu(RenderingContext.ExecuteCommandList(cmdList));
        cmdList.DecrementReference();
    }
    
    public static void Shutdown() {
        Debug.Assert(RenderingContext != null, "RenderingContext != null");

        WhiteTexture.DecrementReference(); WhiteTexture = null!;
        BlackTexture.DecrementReference(); BlackTexture = null!;
        RedTexture.DecrementReference(); RedTexture = null!;
        GreenTexture.DecrementReference(); GreenTexture = null!;
        BlueTexture.DecrementReference(); BlueTexture = null!;
        
        ResourceSignatureStorage.Dispose();
        RenderingContext = null!;
        CompilationFactory = null!;
        
        ShaderCompilationEngine.Shutdown();
    }
}