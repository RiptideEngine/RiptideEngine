namespace Riptide.ShaderCompilation;

public abstract class CompilationPipeline : IDisposable {
    private bool _disposed;

    public abstract CompilationPipeline SetOptimizationLevel(OptimizationLevel optimize);

    public abstract CompilationPipeline ResetDefault();
    
    public CompilationPipeline SetEntrypoint(ReadOnlySpan<char> entrypoint) {
        return RegexValidators.GetIdentifierRegex().IsMatch(entrypoint) ? SetEntrypointImpl(entrypoint) : throw new ArgumentException("Entrypoint must be a valid identifier.", nameof(entrypoint));
    }
    protected abstract CompilationPipeline SetEntrypointImpl(ReadOnlySpan<char> entrypoint);

    public CompilationPipeline SetTarget(ReadOnlySpan<char> target) {
        return RegexValidators.GetTargetRegex().IsMatch(target) ? IsTargetSupported(target) ? SetTargetImpl(target) : throw new NotSupportedException($"Shader target '{target}' is not supported on this compiler.") : throw new ArgumentException("Invalid target argument.", nameof(target));
    }
    protected abstract CompilationPipeline SetTargetImpl(ReadOnlySpan<char> target);

    public abstract CompilationPipeline SetSpecializedArgument(ReadOnlySpan<char> key, ReadOnlySpan<char> value, bool silent = true);

    public abstract CompiledResult Compile(ReadOnlySpan<byte> sourceCode);
    
    public abstract bool IsTargetSupported(ReadOnlySpan<char> target);
    
    protected abstract void DisposeImpl(bool disposing);
    
    protected void Dispose(bool disposing) {
        if (_disposed) return;

        DisposeImpl(disposing);
        _disposed = true;
    }
    
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    ~CompilationPipeline() {
        Dispose(false);
    }
}