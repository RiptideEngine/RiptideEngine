namespace RiptideRendering;

public abstract class ComputeShader : Shader {
    public nint ShaderHandle { get; protected set; }
}