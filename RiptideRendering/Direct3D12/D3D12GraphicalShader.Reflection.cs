using RiptideRendering.Shadering;
using DxcBuffer = Silk.NET.Direct3D.Compilers.Buffer;

namespace RiptideRendering.Direct3D12;

unsafe partial class D3D12GraphicalShader {
    private void InitializeReflection() {
        int hr;

        var cbBuilder = ImmutableArray.CreateBuilder<ConstantBufferInfo>();
        var rorBuilder = ImmutableArray.CreateBuilder<ReadonlyResourceInfo>();
        var rwrBuilder = ImmutableArray.CreateBuilder<ReadWriteResourceInfo>();

        var deduplication = new HashSet<uint>();

        using ComPtr<ID3D12ShaderReflection> pVsReflection = default;
        using ComPtr<ID3D12ShaderReflection> pPsReflection = default;

        hr = DxcCompilation.Utils->CreateReflection(new DxcBuffer() { Ptr = ((IDxcBlob*)VertexShaderHandle)->GetBufferPointer(), Size = ((IDxcBlob*)VertexShaderHandle)->GetBufferSize() }, SilkMarshal.GuidPtrOf<ID3D12ShaderReflection>(), (void**)pVsReflection.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);

        hr = DxcCompilation.Utils->CreateReflection(new DxcBuffer() { Ptr = ((IDxcBlob*)PixelShaderHandle)->GetBufferPointer(), Size = ((IDxcBlob*)PixelShaderHandle)->GetBufferSize() }, SilkMarshal.GuidPtrOf<ID3D12ShaderReflection>(), (void**)pPsReflection.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);

        CollectReflectionInfos(cbBuilder, rorBuilder, rwrBuilder, deduplication, pVsReflection);
        CollectReflectionInfos(cbBuilder, rorBuilder, rwrBuilder, deduplication, pPsReflection);

        if (HullShaderHandle != nint.Zero) {
            using ComPtr<ID3D12ShaderReflection> pHsReflection = default;
            using ComPtr<ID3D12ShaderReflection> pDsReflection = default;

            hr = DxcCompilation.Utils->CreateReflection(new DxcBuffer() { Ptr = ((IDxcBlob*)HullShaderHandle)->GetBufferPointer(), Size = ((IDxcBlob*)HullShaderHandle)->GetBufferSize() }, SilkMarshal.GuidPtrOf<ID3D12ShaderReflection>(), (void**)pHsReflection.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            hr = DxcCompilation.Utils->CreateReflection(new DxcBuffer() { Ptr = ((IDxcBlob*)DomainShaderHandle)->GetBufferPointer(), Size = ((IDxcBlob*)DomainShaderHandle)->GetBufferSize() }, SilkMarshal.GuidPtrOf<ID3D12ShaderReflection>(), (void**)pDsReflection.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            CollectReflectionInfos(cbBuilder, rorBuilder, rwrBuilder, deduplication, pHsReflection);
            CollectReflectionInfos(cbBuilder, rorBuilder, rwrBuilder, deduplication, pDsReflection);
        }

        ConstantBuffers = cbBuilder.ToImmutable();
        ReadonlyResources = rorBuilder.ToImmutable();
        ReadWriteResources = rwrBuilder.ToImmutable();

        static void CollectReflectionInfos(ImmutableArray<ConstantBufferInfo>.Builder cbBuilder, ImmutableArray<ReadonlyResourceInfo>.Builder rorBuilder, ImmutableArray<ReadWriteResourceInfo>.Builder rwrBuilder, HashSet<uint> deduplications, ID3D12ShaderReflection* pReflection) {
            for (uint i = 0; ; i++) {
                ShaderInputBindDesc sibdesc;
                if (pReflection->GetResourceBindingDesc(i, &sibdesc) < 0) break;

                if (!deduplications.Add(Crc32C.Compute(sibdesc.Name, UnsafeHelpers.StringLength(sibdesc.Name)))) continue;

                switch (sibdesc.Type) {
                    case D3DShaderInputType.D3DSitCbuffer:
                        var pBuffer = pReflection->GetConstantBufferByName(sibdesc.Name);
                        if (pBuffer == null) continue;

                        ShaderBufferDesc desc;
                        if (pBuffer->GetDesc(&desc) < 0) continue;

                        cbBuilder.Add(new() {
                            Name = new string((sbyte*)desc.Name),
                            Size = desc.Size,
                            Register = sibdesc.BindPoint,
                            Space = sibdesc.Space,
                        });
                        break;

                    case D3DShaderInputType.D3DSitTbuffer or D3DShaderInputType.D3DSitTexture or D3DShaderInputType.D3DSitStructured or D3DShaderInputType.D3DSitByteaddress: {
                        if (!D3D12Convert.TryConvert(sibdesc.Type, sibdesc.Dimension, out var resourceType)) {
                            throw new NotSupportedException($"Cannot convert the type of resource '{new string((sbyte*)sibdesc.Name)}' {SilkHelper.GetNativeName(sibdesc.Type, "Name")} to it's correspond ResourceType enum.");
                        }

                        rorBuilder.Add(new() {
                            Name = new string((sbyte*)sibdesc.Name),
                            Type = resourceType,
                            Register = sibdesc.BindPoint,
                            Space = sibdesc.Space,
                        });
                        break;
                    }

                    case D3DShaderInputType.D3DSitUavRwtyped or D3DShaderInputType.D3DSitUavRwstructured or D3DShaderInputType.D3DSitUavRwbyteaddress or D3DShaderInputType.D3DSitUavAppendStructured or D3DShaderInputType.D3DSitUavConsumeStructured or D3DShaderInputType.D3DSitUavRwstructuredWithCounter: {
                        if (!D3D12Convert.TryConvert(sibdesc.Type, sibdesc.Dimension, out var resourceType)) {
                            throw new NotSupportedException($"Cannot convert the type of resource '{new string((sbyte*)sibdesc.Name)}' {SilkHelper.GetNativeName(sibdesc.Type, "Name")} to it's correspond ResourceType enum.");
                        }

                        rwrBuilder.Add(new() {
                            Name = new string((sbyte*)sibdesc.Name),
                            Type = resourceType,
                            Register = sibdesc.BindPoint,
                            Space = sibdesc.Space,
                        });
                        break;
                    }

                    case D3DShaderInputType.D3DSitSampler: break;
                }
            }
        }
    }
}