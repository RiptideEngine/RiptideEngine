using Riptide.ShaderCompilation;

namespace RiptideFoundation;

public sealed class ShaderCompilationFactory {
    private readonly Type _compilerType;

    internal ShaderCompilationFactory(RenderingAPI api) {
        _compilerType = api switch {
            RenderingAPI.Direct3D12 => typeof(D3D12CompilationPipeline),
            RenderingAPI.None => throw new ArgumentException($"Unexpected RenderingAPI '{nameof(RenderingAPI.None)}' from a RenderingContext."),
            _ => throw new ArgumentException($"Cannot initialize shader compilation pipeline for target API '{api}'.")
        };
    }
    
    public CompilationPipeline CreateCompilationPipeline() {
        return Unsafe.As<CompilationPipeline>(Activator.CreateInstance(_compilerType, true)!);
    }
}