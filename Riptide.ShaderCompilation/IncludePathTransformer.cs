namespace Riptide.ShaderCompilation;

public abstract class IncludePathTransformer {
    public abstract string? Transform(ReadOnlySpan<char> path);
}