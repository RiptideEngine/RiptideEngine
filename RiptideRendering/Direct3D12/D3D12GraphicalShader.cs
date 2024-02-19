namespace RiptideRendering.Direct3D12;

internal sealed class D3D12GraphicalShader : GraphicalShader {
    private byte[] _vsBytecode, _psBytecode, _dsBytecode, _hsBytecode;

    public ReadOnlySpan<byte> VSBytecode => _vsBytecode;
    public ReadOnlySpan<byte> PSBytecode => _psBytecode;
    public ReadOnlySpan<byte> DSBytecode => _dsBytecode;
    public ReadOnlySpan<byte> HSBytecode => _hsBytecode;
    
    public D3D12GraphicalShader(ReadOnlySpan<byte> vsBytecode, ReadOnlySpan<byte> psBytecode) {
        _vsBytecode = vsBytecode.ToArray();
        _psBytecode = psBytecode.ToArray();
        _dsBytecode = _hsBytecode = [];

        _refcount = 1;
    }

    public D3D12GraphicalShader(ReadOnlySpan<byte> vsBytecode, ReadOnlySpan<byte> hsBytecode, ReadOnlySpan<byte> dsBytecode, ReadOnlySpan<byte> psBytecode) {
        _vsBytecode = vsBytecode.ToArray();
        _hsBytecode = hsBytecode.ToArray();
        _dsBytecode = dsBytecode.ToArray();
        _psBytecode = psBytecode.ToArray();

        _refcount = 1;
    }

    protected override void Dispose() {
        _vsBytecode = _psBytecode = _hsBytecode = _dsBytecode = [];
    }
}