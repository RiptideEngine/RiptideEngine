using RiptideRendering.Shadering;
using Silk.NET.Direct3D.Compilers;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe partial class D3D12GraphicalShader : GraphicalShader {
    private ID3D12RootSignature* pRootSignature;
    private ID3D12VersionedRootSignatureDeserializer* pRootSignatureDeserializer;

    public ID3D12RootSignature* RootSignature => pRootSignature;

    public D3D12GraphicalShader(D3D12RenderingContext context, ReadOnlySpan<byte> rootSigBytecode, ReadOnlySpan<byte> vsBytecode, ReadOnlySpan<byte> psBytecode, ReadOnlySpan<byte> hsBytecode, ReadOnlySpan<byte> dsBytecode) {
        DxcCompilation.AssertInitialized();

        try {
            int hr;

            using ComPtr<IDxcBlob> pVsBlob = default;
            using ComPtr<IDxcBlob> pPsBlob = default;
            using ComPtr<IDxcBlob> pHsBlob = default;
            using ComPtr<IDxcBlob> pDsBlob = default;

            hr = DxcCompilation.Utils->CreateBlob(Unsafe.AsPointer(ref MemoryMarshal.GetReference(vsBytecode)), (uint)vsBytecode.Length, 0, (IDxcBlobEncoding**)pVsBlob.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            hr = DxcCompilation.Utils->CreateBlob(Unsafe.AsPointer(ref MemoryMarshal.GetReference(psBytecode)), (uint)psBytecode.Length, 0, (IDxcBlobEncoding**)pPsBlob.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            if (!hsBytecode.IsEmpty) {
                hr = DxcCompilation.Utils->CreateBlob(Unsafe.AsPointer(ref MemoryMarshal.GetReference(hsBytecode)), (uint)hsBytecode.Length, 0, (IDxcBlobEncoding**)pHsBlob.GetAddressOf());
                Marshal.ThrowExceptionForHR(hr);

                hr = DxcCompilation.Utils->CreateBlob(Unsafe.AsPointer(ref MemoryMarshal.GetReference(dsBytecode)), (uint)dsBytecode.Length, 0, (IDxcBlobEncoding**)pDsBlob.GetAddressOf());
                Marshal.ThrowExceptionForHR(hr);
            }

            fixed (byte* pRootSig = rootSigBytecode) {
                using ComPtr<ID3D12RootSignature> pOutputRootSig = default;
                using ComPtr<ID3D12VersionedRootSignatureDeserializer> pOutputDeserializer = default;

                context.RootSigStorage.Get(pRootSig, (nuint)rootSigBytecode.Length, pOutputRootSig.GetAddressOf(), pOutputDeserializer.GetAddressOf());

                pRootSignature = pOutputRootSig.Detach();
                pRootSignatureDeserializer = pOutputDeserializer.Detach();
            }

            VertexShaderHandle = (nint)pVsBlob.Detach();
            PixelShaderHandle = (nint)pPsBlob.Detach();
            HullShaderHandle = (nint)pHsBlob.Detach();
            DomainShaderHandle = (nint)pDsBlob.Detach();

            InitializeReflection();
        } catch {
            Dispose();
            throw;
        }

        _refcount = 1;
    }

    public VersionedRootSignatureDesc* GetRootSignatureDesc() {
        VersionedRootSignatureDesc* ret;
        int hr = pRootSignatureDeserializer->GetRootSignatureDescAtVersion(D3DRootSignatureVersion.Version10, &ret);
        Debug.Assert(hr >= 0);

        return ret;
    }

    protected override void Dispose() {
        _reflector = null!;

        ((IDxcBlob*)VertexShaderHandle)->Release(); VertexShaderHandle = nint.Zero;
        ((IDxcBlob*)PixelShaderHandle)->Release(); PixelShaderHandle = nint.Zero;

        if (HullShaderHandle != nint.Zero) {
            ((IDxcBlob*)HullShaderHandle)->Release(); HullShaderHandle = nint.Zero;
            ((IDxcBlob*)DomainShaderHandle)->Release(); DomainShaderHandle = nint.Zero;
        }

        pRootSignature = null;
        pRootSignatureDeserializer = null;
    }
}