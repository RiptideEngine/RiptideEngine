namespace RiptideRendering;

public abstract class GraphicalShader : Shader {
    public nint VertexShaderHandle { get; protected set; }
    public nint PixelShaderHandle { get; protected set; }
    public nint HullShaderHandle { get; protected set; }
    public nint DomainShaderHandle { get; protected set; }

    public bool HasTessellationStages { [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] get => HullShaderHandle != nint.Zero; }
}