namespace RiptideRendering.Direct3D12;

internal sealed class D3D12ComputeShader(ReadOnlySpan<byte> bytecode) : ComputeShader {
    private byte[] _bytecode = bytecode.ToArray();
    
    public ReadOnlySpan<byte> Bytecode => _bytecode;

    protected override void Dispose() {
        _bytecode = [];
    }
}