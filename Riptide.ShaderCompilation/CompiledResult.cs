using RiptideEngine.Core;

namespace Riptide.ShaderCompilation;

public abstract class CompiledResult : ReferenceCounted {
    public abstract CompiledBytecode GetShaderBytecode();
    public abstract ReflectInformation GetReflectionInfo();
}