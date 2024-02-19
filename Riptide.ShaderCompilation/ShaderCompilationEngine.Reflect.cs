namespace Riptide.ShaderCompilation;

unsafe partial class ShaderCompilationEngine {
    public static ShaderReflection CreateReflection(ReflectInformation info) {
        return info switch {
            D3D12CompilationPipeline.DxilReflectInformation d3d12info => new D3D12ShaderReflection(d3d12info),
            _ => null!,
        };
    }
}