namespace RiptideRendering.Direct3D12;

internal sealed unsafe class RootSignatureStorage : IDisposable {
    private struct Entry : IDisposable {
        public ID3D12RootSignature* RootSignature;
        public ID3D12VersionedRootSignatureDeserializer* Deserializer;

        public Entry(ID3D12RootSignature* pRootSig, ID3D12VersionedRootSignatureDeserializer* pDeserializer) {
            RootSignature = pRootSig;
            Deserializer = pDeserializer;
        }

        public void Dispose() {
            RootSignature->Release();
            Deserializer->Release();
        }
    }

    private readonly Dictionary<uint, Entry> _entries;
    private readonly object _lock;

    private D3D12RenderingContext _context;

    public RootSignatureStorage(D3D12RenderingContext context) {
        _entries = new();
        _lock = new();

        _context = context;
    }

    public void Get(ReadOnlySpan<byte> bytecode, ID3D12RootSignature** ppOutputSignature, ID3D12VersionedRootSignatureDeserializer** ppOutputDeserializer) {
        Get(Unsafe.AsPointer(ref MemoryMarshal.GetReference(bytecode)), (nuint)bytecode.Length, ppOutputSignature, ppOutputDeserializer);
    }

    public void Get(void* pBytecode, nuint bytecodeSize, ID3D12RootSignature** ppOutputSignature, ID3D12VersionedRootSignatureDeserializer** ppOutputDeserializer) {
        uint hash = Crc32C.Compute((byte*)pBytecode, bytecodeSize);
        int hr;

        lock (_lock) {
            if (_entries.TryGetValue(hash, out var entry)) {
                *ppOutputSignature = entry.RootSignature;
                *ppOutputDeserializer = entry.Deserializer;

                return;
            }

            using ComPtr<ID3D12VersionedRootSignatureDeserializer> pOutputDeserializer = default;

            hr = _context.D3D12.CreateVersionedRootSignatureDeserializer(pBytecode, bytecodeSize, SilkMarshal.GuidPtrOf<ID3D12VersionedRootSignatureDeserializer>(), (void**)pOutputDeserializer.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            VersionedRootSignatureDesc* vdesc;
            hr = pOutputDeserializer.GetRootSignatureDescAtVersion(D3DRootSignatureVersion.Version10, &vdesc);
            Marshal.ThrowExceptionForHR(hr);

            // Validate root signature
            {
                for (uint i = 0; i < vdesc->Desc10.NumParameters; i++) {
                    ref readonly var param = ref vdesc->Desc10.PParameters[i];

                    if (param.ParameterType != RootParameterType.TypeDescriptorTable) continue;

                    ref readonly var table = ref param.DescriptorTable;

                    if (table.PDescriptorRanges[table.NumDescriptorRanges - 1].NumDescriptors == uint.MaxValue) throw new NotSupportedException("Unbounded number of descriptor is not supported.");

                    for (uint r = 0; r < table.NumDescriptorRanges; r++) {
                        ref readonly var range = ref table.PDescriptorRanges[r];

                        if (range.OffsetInDescriptorsFromTableStart != uint.MaxValue) throw new NotSupportedException("Explicit descriptor offset is not supported.");
                    }
                }
            }

            using ComPtr<ID3D12RootSignature> pOutputRootSig = default;
            hr = _context.Device->CreateRootSignature(1, pBytecode, bytecodeSize, SilkMarshal.GuidPtrOf<ID3D12RootSignature>(), (void**)pOutputRootSig.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            _entries.Add(hash, new Entry(pOutputRootSig.Handle, pOutputDeserializer.Handle));

            *ppOutputSignature = pOutputRootSig.Detach();
            *ppOutputDeserializer = pOutputDeserializer.Detach();
        }
    }

    private void Dispose(bool disposing) {
        if (_context == null) return;

        if (disposing) { }

        lock (_lock) {
            foreach ((_, var entry) in _entries) {
                entry.Dispose();
            }
            _entries.Clear();
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