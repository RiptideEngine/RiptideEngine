namespace RiptideRendering.Direct3D12;

internal sealed unsafe class RootSignatureStorage : IDisposable {
    private readonly Dictionary<int, nint> _pool;
    private readonly object _lock;

    private D3D12RenderingContext _context;

    public RootSignatureStorage(D3D12RenderingContext context) {
        _pool = new();
        _lock = new();

        _context = context;
    }

    public void Get(RootSignatureDesc desc, ID3D12RootSignature** ppOutputSignature) {
        int hash = HashCode.Combine(desc.NumParameters, desc.NumStaticSamplers, desc.Flags);

        for (uint i = 0; i < desc.NumParameters; i++) {
            ref readonly var param = ref desc.PParameters[i];
            hash = HashCode.Combine(hash, param.ParameterType, param.ShaderVisibility);

            switch (param.ParameterType) {
                case RootParameterType.TypeDescriptorTable:
                    ref readonly var table = ref param.DescriptorTable;
                    hash = HashCode.Combine(hash, table.NumDescriptorRanges);

                    for (uint r = 0; r < table.NumDescriptorRanges; r++) {
                        ref readonly var range = ref table.PDescriptorRanges[r];

                        hash = HashCode.Combine(hash, range.RangeType, range.BaseShaderRegister, range.RegisterSpace, range.NumDescriptors, range.OffsetInDescriptorsFromTableStart);
                    }
                    break;

                case RootParameterType.Type32BitConstants:
                    ref readonly var constants = ref param.Constants;

                    hash = HashCode.Combine(hash, constants.ShaderRegister, constants.RegisterSpace, constants.Num32BitValues);
                    break;

                case RootParameterType.TypeCbv or RootParameterType.TypeSrv or RootParameterType.TypeUav:
                    ref readonly var descriptor = ref param.Descriptor;

                    hash = HashCode.Combine(hash, descriptor.ShaderRegister, descriptor.RegisterSpace);
                    break;
            }
        }

        for (uint i = 0; i < desc.NumStaticSamplers; i++) {
            ref readonly var sdesc = ref desc.PStaticSamplers[i];

            hash = HashCode.Combine(hash, sdesc.Filter, sdesc.AddressU, sdesc.AddressV, sdesc.AddressW, sdesc.MaxAnisotropy);
            hash = HashCode.Combine(hash, sdesc.ComparisonFunc);
            hash = HashCode.Combine(hash, sdesc.MinLOD, sdesc.MaxLOD, sdesc.MipLODBias);
            hash = HashCode.Combine(hash, sdesc.BorderColor);
            hash = HashCode.Combine(hash, sdesc.ShaderRegister, sdesc.RegisterSpace, sdesc.ShaderVisibility);
        }

        int hr;

        lock (_lock) {
            if (_pool.TryGetValue(hash, out var entry)) {
                *ppOutputSignature = (ID3D12RootSignature*)entry;

                return;
            }

            using ComPtr<ID3D10Blob> pSerialized = default;
            using ComPtr<ID3D10Blob> pError = default;
            hr = _context.D3D12.SerializeRootSignature(&desc, D3DRootSignatureVersion.Version10, pSerialized.GetAddressOf(), pError.GetAddressOf());
            if (hr < 0) {
                throw new Exception($"Failed to serialize Root Signature. Error: '{new string((sbyte*)pError.GetBufferPointer(), 0, (int)pError.GetBufferSize())}'.");
            }

            ID3D12RootSignature* pRoot;
            hr = _context.Device->CreateRootSignature(1, pSerialized.GetBufferPointer(), pSerialized.GetBufferSize(), SilkMarshal.GuidPtrOf<ID3D12RootSignature>(), (void**)&pRoot);
            Marshal.ThrowExceptionForHR(hr);

            _pool.Add(hash, (nint)pRoot);

            *ppOutputSignature = pRoot;
        }
    }

    private void Dispose(bool disposing) {
        if (_context == null) return;

        if (disposing) { }

        lock (_lock) {
            foreach ((_, var entry) in _pool) {
                ((ID3D12RootSignature*)entry)->Release();
            }
            _pool.Clear();
        }

        _context = null!;
    }

    ~RootSignatureStorage() {
        Dispose(disposing: false);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}