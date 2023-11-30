using Riptide.ShaderCompilation;

namespace RiptideFoundation;

public static class ShaderCompilationPipeline {
    public static CompilationPipeline Pipeline { get; private set; } = null!;
    
    public static RenderingAPI TargetApi { get; private set; }
    
    internal static void Initialize(RenderingContext context) {
        if (Pipeline != null) throw new InvalidOperationException("Shader compilation pipeline has already initialized.");

        switch (context.RenderingAPI) {
            case RenderingAPI.Direct3D12:
                Pipeline = new DxcCompilationPipeline();
                TargetApi = RenderingAPI.Direct3D12;
                break;
            
            case RenderingAPI.None: throw new ArgumentException($"Unexpected RenderingAPI '{nameof(RenderingAPI.None)}' from a RenderingContext.");
            
            default: throw new ArgumentException($"Cannot initialize shader compilation pipeline for target API '{context.RenderingAPI}'.");
        }
    }

    internal static void Shutdown() {
        if (Pipeline == null) return;
        
        Pipeline.Dispose();
        Pipeline = null!;

        TargetApi = RenderingAPI.None;
    }
}