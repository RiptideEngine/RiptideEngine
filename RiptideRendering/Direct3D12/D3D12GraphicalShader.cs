namespace RiptideRendering.Direct3D12;

internal sealed unsafe partial class D3D12GraphicalShader : GraphicalShader {
    private byte[] _vsBytecode, _psBytecode, _dsBytecode, _hsBytecode;

    public ReadOnlySpan<byte> VSBytecode => _vsBytecode;
    public ReadOnlySpan<byte> PSBytecode => _psBytecode;
    public ReadOnlySpan<byte> DSBytecode => _dsBytecode;
    public ReadOnlySpan<byte> HSBytecode => _hsBytecode;
    
    public D3D12GraphicalShader(D3D12RenderingContext context, ReadOnlySpan<byte> vsBytecode, ReadOnlySpan<byte> psBytecode, ReadOnlySpan<byte> hsBytecode, ReadOnlySpan<byte> dsBytecode) {
        _vsBytecode = vsBytecode.ToArray();
        _psBytecode = psBytecode.ToArray();
        _dsBytecode = dsBytecode.ToArray();
        _hsBytecode = hsBytecode.ToArray();

        _refcount = 1;
    }

    protected override void Dispose() {
        ConstantBuffers = [];
        ReadonlyResources = [];
        ReadWriteResources = [];

        _vsBytecode = _psBytecode = _hsBytecode = _dsBytecode = [];
    }
}