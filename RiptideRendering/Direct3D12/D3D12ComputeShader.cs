using RiptideRendering.Shadering;
using Silk.NET.Direct3D.Compilers;

using DxcBuffer = Silk.NET.Direct3D.Compilers.Buffer;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12ComputeShader : ComputeShader {
    private ID3D12RootSignature* pRootSignature;
    private ID3D12VersionedRootSignatureDeserializer* pRootSignatureDeserializer;
    private D3D12ShaderReflector _reflector = null!;

    public ID3D12RootSignature* RootSignature => pRootSignature;
    public override ShaderReflector Reflector => _reflector;

    public D3D12ComputeShader(D3D12RenderingContext context, ReadOnlySpan<byte> bytecode, ReadOnlySpan<byte> rootSignatureBytecode) {
        try {
            int hr;

            using ComPtr<IDxcBlob> pBlob = default;
            hr = DxcCompilation.Utils->CreateBlob(Unsafe.AsPointer(ref MemoryMarshal.GetReference(bytecode)), (uint)bytecode.Length, 0, (IDxcBlobEncoding**)pBlob.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            fixed (byte* pRootSig = rootSignatureBytecode) {
                using ComPtr<ID3D12RootSignature> pOutputRootSig = default;
                using ComPtr<ID3D12VersionedRootSignatureDeserializer> pOutputDeserializer = default;

                context.RootSigStorage.Get(pRootSig, (nuint)rootSignatureBytecode.Length, pOutputRootSig.GetAddressOf(), pOutputDeserializer.GetAddressOf());

                pRootSignature = pOutputRootSig.Detach();
                pRootSignatureDeserializer = pOutputDeserializer.Detach();
            }

            ShaderHandle = (nint)pBlob.Detach();

            InitializeReflection();
        } catch {
            Dispose();
            throw;
        }
    }

    private void InitializeReflection() {
        int hr;

        using ComPtr<ID3D12ShaderReflection> pReflection = default;

        hr = DxcCompilation.Utils->CreateReflection(new DxcBuffer() { Ptr = ((IDxcBlob*)ShaderHandle)->GetBufferPointer(), Size = ((IDxcBlob*)ShaderHandle)->GetBufferSize() }, SilkMarshal.GuidPtrOf<ID3D12ShaderReflection>(), (void**)pReflection.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);

        VersionedRootSignatureDesc* rootSigDesc;
        hr = pRootSignatureDeserializer->GetRootSignatureDescAtVersion(D3DRootSignatureVersion.Version10, &rootSigDesc);
        Debug.Assert(hr >= 0);

        _reflector = new(pReflection, rootSigDesc->Desc10);
    }

    public VersionedRootSignatureDesc* GetRootSignatureDesc() {
        VersionedRootSignatureDesc* ret;
        int hr = pRootSignatureDeserializer->GetRootSignatureDescAtVersion(D3DRootSignatureVersion.Version10, &ret);
        Debug.Assert(hr >= 0);

        return ret;
    }

    protected override void Dispose() {
        ((IDxcBlob*)ShaderHandle)->Release(); ShaderHandle = nint.Zero;

        _reflector = null!;
        pRootSignature = null;
        pRootSignatureDeserializer = null;
    }
}