using RiptideRendering.Shadering;
using Silk.NET.Direct3D.Compilers;

using DxcBuffer = Silk.NET.Direct3D.Compilers.Buffer;

namespace RiptideRendering.Direct3D12;

unsafe partial class D3D12GraphicalShader {
    private D3D12ShaderReflector _reflector = null!;

    public override ShaderReflector Reflector => _reflector;

    private void InitializeReflection() {
        int hr;

        using ComPtr<ID3D12ShaderReflection> pVsReflection = default;
        using ComPtr<ID3D12ShaderReflection> pPsReflection = default;
        using ComPtr<ID3D12ShaderReflection> pHsReflection = default;
        using ComPtr<ID3D12ShaderReflection> pDsReflection = default;

        hr = DxcCompilation.Utils->CreateReflection(new DxcBuffer() { Ptr = ((IDxcBlob*)VertexShaderHandle)->GetBufferPointer(), Size = ((IDxcBlob*)VertexShaderHandle)->GetBufferSize() }, SilkMarshal.GuidPtrOf<ID3D12ShaderReflection>(), (void**)pVsReflection.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);

        hr = DxcCompilation.Utils->CreateReflection(new DxcBuffer() { Ptr = ((IDxcBlob*)PixelShaderHandle)->GetBufferPointer(), Size = ((IDxcBlob*)PixelShaderHandle)->GetBufferSize() }, SilkMarshal.GuidPtrOf<ID3D12ShaderReflection>(), (void**)pPsReflection.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);

        if (HullShaderHandle != nint.Zero) {
            hr = DxcCompilation.Utils->CreateReflection(new DxcBuffer() { Ptr = ((IDxcBlob*)HullShaderHandle)->GetBufferPointer(), Size = ((IDxcBlob*)HullShaderHandle)->GetBufferSize() }, SilkMarshal.GuidPtrOf<ID3D12ShaderReflection>(), (void**)pHsReflection.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            hr = DxcCompilation.Utils->CreateReflection(new DxcBuffer() { Ptr = ((IDxcBlob*)DomainShaderHandle)->GetBufferPointer(), Size = ((IDxcBlob*)DomainShaderHandle)->GetBufferSize() }, SilkMarshal.GuidPtrOf<ID3D12ShaderReflection>(), (void**)pDsReflection.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);
        }

        VersionedRootSignatureDesc* rootSigDesc;
        hr = pRootSignatureDeserializer->GetRootSignatureDescAtVersion(D3DRootSignatureVersion.Version10, &rootSigDesc);
        Debug.Assert(hr >= 0);

        ref readonly var rootSigDesc10 = ref rootSigDesc->Desc10;

        if (pHsReflection.Handle != null) {
            ID3D12ShaderReflection** ppReflections = stackalloc ID3D12ShaderReflection*[4] {
                pVsReflection,
                pPsReflection,
                pHsReflection,
                pDsReflection,
            };
            _reflector = new(ppReflections, 4, rootSigDesc10);
        } else {
            ID3D12ShaderReflection** ppReflections = stackalloc ID3D12ShaderReflection*[2] {
                pVsReflection,
                pPsReflection,
            };
            _reflector = new(ppReflections, 2, rootSigDesc10);
        }
    }
}