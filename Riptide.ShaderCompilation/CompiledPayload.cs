using Silk.NET.Direct3D.Compilers;

namespace Riptide.ShaderCompilation;

public abstract class CompiledPayload : RiptideRcObject {
    public abstract ReadOnlySpan<byte> GetData();
}

internal sealed unsafe class DxcCompiledPayload : CompiledPayload {
    private IDxcBlob* pBlob;

    public DxcCompiledPayload(IDxcBlob* blob) {
        pBlob = blob;
        pBlob->AddRef();

        _refcount = 1;
    }
    
    public override ReadOnlySpan<byte> GetData() => new((byte*)pBlob->GetBufferPointer(), (int)pBlob->GetBufferSize());

    protected override void Dispose() {
        pBlob->Release();
        pBlob = null!;
    }
}