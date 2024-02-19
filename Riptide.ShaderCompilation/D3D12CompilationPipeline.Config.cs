namespace Riptide.ShaderCompilation;

partial class D3D12CompilationPipeline {
    public override CompilationPipeline SetEntrypoint(ReadOnlySpan<char> entrypoint) {
        if (!ArgumentValidator.GetEntrypointValidator().IsMatch(entrypoint)) throw new ArgumentException($"Invalid entrypoint '{entrypoint}'.");

        _entrypoint = $"{entrypoint}\0";
        return this;
    }
    public override CompilationPipeline SetTarget(ReadOnlySpan<char> target) {
        if (!ArgumentValidator.GetDxcTargetValidator().IsMatch(target)) throw new ArgumentException($"Invalid target '{target}'.");

        _target = $"{target}\0";
        return this;
    }
    public override CompilationPipeline SetOptimizationLevel(OptimizationLevel optimize) {
        _optimize = optimize is < OptimizationLevel.Disable or > OptimizationLevel.Level3 ? OptimizationLevel.Level1 : optimize;
        return this;
    }

    public override CompilationPipeline SetIncludePathTransformer(IncludePathTransformer? transformer) {
        _transformer = transformer;
        return this;
    }
}