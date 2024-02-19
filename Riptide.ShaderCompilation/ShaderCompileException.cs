namespace Riptide.ShaderCompilation;

public sealed class ShaderCompileException : Exception {
    public ShaderCompileException(string message) : base(message) { }
    public ShaderCompileException(string message, Exception? inner) : base(message, inner) { }
}