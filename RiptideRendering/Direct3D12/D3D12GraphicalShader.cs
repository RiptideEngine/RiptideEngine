using RiptideRendering.Shadering;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe partial class D3D12GraphicalShader : GraphicalShader {
    public D3D12GraphicalShader(D3D12RenderingContext context, ReadOnlySpan<byte> vsBytecode, ReadOnlySpan<byte> psBytecode, ReadOnlySpan<byte> hsBytecode, ReadOnlySpan<byte> dsBytecode) {
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

    protected override void Dispose() {
        ConstantBuffers = [];
        ReadonlyResources = [];
        ReadWriteResources = [];

        ((IDxcBlob*)VertexShaderHandle)->Release(); VertexShaderHandle = nint.Zero;
        ((IDxcBlob*)PixelShaderHandle)->Release(); PixelShaderHandle = nint.Zero;

        if (HullShaderHandle != nint.Zero) {
            ((IDxcBlob*)HullShaderHandle)->Release(); HullShaderHandle = nint.Zero;
            ((IDxcBlob*)DomainShaderHandle)->Release(); DomainShaderHandle = nint.Zero;
        }
    }
}