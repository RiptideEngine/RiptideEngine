using RiptideEngine.Core;

namespace Riptide.ShaderCompilation;

public abstract class CompiledBytecode : ReferenceCounted {
    public abstract ReadOnlySpan<byte> GetData();
}