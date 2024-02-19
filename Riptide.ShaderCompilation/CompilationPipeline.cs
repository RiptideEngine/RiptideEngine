namespace Riptide.ShaderCompilation;

public enum OptimizationLevel {
    Disable = 0,
    
    Level0 = 1,
    Level1 = 2,
    Level2 = 3,
    Level3 = 4,
}

public abstract class CompilationPipeline {
    public abstract CompilationPipeline SetEntrypoint(ReadOnlySpan<char> entrypoint);
    public abstract CompilationPipeline SetTarget(ReadOnlySpan<char> target);
    public abstract CompilationPipeline SetOptimizationLevel(OptimizationLevel optimize);
    public abstract CompilationPipeline SetIncludePathTransformer(IncludePathTransformer? transformer);
    
    public abstract CompiledResult Compile(ReadOnlySpan<byte> source);
}