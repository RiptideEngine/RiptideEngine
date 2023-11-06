namespace RiptideRendering.Direct3D12;

internal static unsafe class D3D12Convert {
    public static bool TryConvert(ComparisonOperator input, out ComparisonFunc output) {
        if (!input.IsDefined()) {
            output = default;
            return false;
        }

        output = (ComparisonFunc)(input + 1);
        return true;
    }

    public static bool TryConvert(ResourceRangeType input, out DescriptorRangeType output) {
        switch (input) {
            case ResourceRangeType.ConstantBuffer: output = DescriptorRangeType.Cbv; return true;
            case ResourceRangeType.ReadonlyResource: output = DescriptorRangeType.Srv; return true;
            case ResourceRangeType.UnorderedAccess: output = DescriptorRangeType.Uav; return true;
            case ResourceRangeType.Sampler: output = DescriptorRangeType.Sampler; return true;
        }

        output = default;
        return false;
    }

    public static bool TryConvert(ShaderModel input, out D3DShaderModel output) {
        switch (input) {
            case ShaderModel.SM6_3: output = D3DShaderModel.ShaderModel63; return true;
            case ShaderModel.SM6_4: output = D3DShaderModel.ShaderModel64; return true;
            case ShaderModel.SM6_5: output = D3DShaderModel.ShaderModel65; return true;
            case ShaderModel.SM6_6: output = D3DShaderModel.ShaderModel66; return true;
            case ShaderModel.SM6_7: output = D3DShaderModel.ShaderModel67; return true;
            default:
                if (input.IsDefined()) {
                    throw new NotImplementedException($"Unimplemented case '{input}'.");
                }

                output = default;
                return false;
        }
    }
    public static bool TryConvert(D3DShaderModel input, out ShaderModel output) {
        switch (input) {
            case D3DShaderModel.ShaderModel63: output = ShaderModel.SM6_3; return true;
            case D3DShaderModel.ShaderModel64: output = ShaderModel.SM6_4; return true;
            case D3DShaderModel.ShaderModel65: output = ShaderModel.SM6_5; return true;
            case D3DShaderModel.ShaderModel66: output = ShaderModel.SM6_6; return true;
            case D3DShaderModel.ShaderModel67: output = ShaderModel.SM6_7; return true;
            default: output = default; return false;
        }
    }
    public static bool TryConvert(Format input, out GraphicsFormat output) {
        switch (input) {
            case Format.FormatUnknown: output = GraphicsFormat.Unknown; return true;

            case Format.FormatR8Sint: output = GraphicsFormat.R8Int; return true;
            case Format.FormatR8Uint: output = GraphicsFormat.R8UInt; return true;
            case Format.FormatR8SNorm: output = GraphicsFormat.R8Norm; return true;
            case Format.FormatR8Unorm: output = GraphicsFormat.R8UNorm; return true;

            case Format.FormatR16Sint: output = GraphicsFormat.R16Int; return true;
            case Format.FormatR16Uint: output = GraphicsFormat.R16UInt; return true;
            case Format.FormatR16SNorm: output = GraphicsFormat.R16Norm; return true;
            case Format.FormatR16Unorm: output = GraphicsFormat.R16UNorm; return true;

            case Format.FormatR32Sint: output = GraphicsFormat.R32Int; return true;
            case Format.FormatR32Uint: output = GraphicsFormat.R32UInt; return true;
            case Format.FormatR32Float: output = GraphicsFormat.R32Float; return true;

            case Format.FormatR8G8Sint: output = GraphicsFormat.R8G8Int; return true;
            case Format.FormatR8G8Uint: output = GraphicsFormat.R8G8UInt; return true;
            case Format.FormatR8G8SNorm: output = GraphicsFormat.R8G8Norm; return true;
            case Format.FormatR8G8Unorm: output = GraphicsFormat.R8G8UNorm; return true;

            case Format.FormatR16G16Sint: output = GraphicsFormat.R16G16Int; return true;
            case Format.FormatR16G16Uint: output = GraphicsFormat.R16G16UInt; return true;
            case Format.FormatR16G16SNorm: output = GraphicsFormat.R16G16Norm; return true;
            case Format.FormatR16G16Unorm: output = GraphicsFormat.R16G16UNorm; return true;

            case Format.FormatR32G32Sint: output = GraphicsFormat.R32G32Int; return true;
            case Format.FormatR32G32Uint: output = GraphicsFormat.R32G32UInt; return true;
            case Format.FormatR32G32Float: output = GraphicsFormat.R32G32Float; return true;

            case Format.FormatB5G6R5Unorm: output = GraphicsFormat.B5G6R5UNorm; return true;

            case Format.FormatR32G32B32Sint: output = GraphicsFormat.R32G32B32Int; return true;
            case Format.FormatR32G32B32Uint: output = GraphicsFormat.R32G32B32UInt; return true;
            case Format.FormatR32G32B32Float: output = GraphicsFormat.R32G32B32Float; return true;

            case Format.FormatR11G11B10Float: output = GraphicsFormat.R11G11B10Float; return true;

            case Format.FormatB4G4R4A4Unorm: output = GraphicsFormat.B4G4R4A4UNorm; return true;

            case Format.FormatR8G8B8A8Sint: output = GraphicsFormat.R8G8B8A8Int; return true;
            case Format.FormatR8G8B8A8Uint: output = GraphicsFormat.R8G8B8A8UInt; return true;
            case Format.FormatR8G8B8A8SNorm: output = GraphicsFormat.R8G8B8A8Norm; return true;
            case Format.FormatR8G8B8A8Unorm: output = GraphicsFormat.R8G8B8A8UNorm; return true;

            case Format.FormatB8G8R8A8Unorm: output = GraphicsFormat.B8G8R8A8UNorm; return true;

            case Format.FormatR16G16B16A16Sint: output = GraphicsFormat.R16G16B16A16Int; return true;
            case Format.FormatR16G16B16A16Uint: output = GraphicsFormat.R16G16B16A16UInt; return true;
            case Format.FormatR16G16B16A16SNorm: output = GraphicsFormat.R16G16B16A16Norm; return true;
            case Format.FormatR16G16B16A16Unorm: output = GraphicsFormat.R16G16B16A16UNorm; return true;

            case Format.FormatR32G32B32A32Sint: output = GraphicsFormat.R32G32B32A32Int; return true;
            case Format.FormatR32G32B32A32Uint: output = GraphicsFormat.R32G32B32A32UInt; return true;
            case Format.FormatR32G32B32A32Float: output = GraphicsFormat.R32G32B32A32Float; return true;

            case Format.FormatR10G10B10A2Unorm: output = GraphicsFormat.R10G10B10A2UNorm; return true;

            case Format.FormatA8Unorm: output = GraphicsFormat.Alpha8; return true;

            case Format.FormatD16Unorm: output = GraphicsFormat.D16UNorm; return true;
            case Format.FormatD24UnormS8Uint: output = GraphicsFormat.D24UNormS8UInt; return true;
            case Format.FormatD32Float: output = GraphicsFormat.D32Float; return true;
            case Format.FormatD32FloatS8X24Uint: output = GraphicsFormat.D32FloatS8UInt; return true;

            default: output = default; return false;
        }
    }
    public static bool TryConvert(GraphicsFormat input, out Format output) {
        switch (input) {
            case GraphicsFormat.Unknown: output = Format.FormatUnknown; return true;

            case GraphicsFormat.R8Int: output = Format.FormatR8Sint; return true;
            case GraphicsFormat.R8UInt: output = Format.FormatR8Uint; return true;
            case GraphicsFormat.R8Norm: output = Format.FormatR8SNorm; return true;
            case GraphicsFormat.R8UNorm: output = Format.FormatR8Unorm; return true;

            case GraphicsFormat.R16Int: output = Format.FormatR16Sint; return true;
            case GraphicsFormat.R16UInt: output = Format.FormatR16Uint; return true;
            case GraphicsFormat.R16Norm: output = Format.FormatR16SNorm; return true;
            case GraphicsFormat.R16UNorm: output = Format.FormatR16Unorm; return true;

            case GraphicsFormat.R32Int: output = Format.FormatR32Sint; return true;
            case GraphicsFormat.R32UInt: output = Format.FormatR32Uint; return true;
            case GraphicsFormat.R32Float: output = Format.FormatR32Float; return true;

            case GraphicsFormat.R8G8Int: output = Format.FormatR8G8Sint; return true;
            case GraphicsFormat.R8G8UInt: output = Format.FormatR8G8Uint; return true;
            case GraphicsFormat.R8G8Norm: output = Format.FormatR8G8SNorm; return true;
            case GraphicsFormat.R8G8UNorm: output = Format.FormatR8G8Unorm; return true;

            case GraphicsFormat.R16G16Int: output = Format.FormatR16G16Sint; return true;
            case GraphicsFormat.R16G16UInt: output = Format.FormatR16G16Uint; return true;
            case GraphicsFormat.R16G16Norm: output = Format.FormatR16G16SNorm; return true;
            case GraphicsFormat.R16G16UNorm: output = Format.FormatR16G16Unorm; return true;

            case GraphicsFormat.R32G32Int: output = Format.FormatR32G32Sint; return true;
            case GraphicsFormat.R32G32UInt: output = Format.FormatR32G32Uint; return true;
            case GraphicsFormat.R32G32Float: output = Format.FormatR32G32Float; return true;

            case GraphicsFormat.B5G6R5UNorm: output = Format.FormatB5G6R5Unorm; return true;
            case GraphicsFormat.B5G5R5A1UNorm: output = Format.FormatB5G5R5A1Unorm; return true;

            case GraphicsFormat.R32G32B32Int: output = Format.FormatR32G32B32Sint; return true;
            case GraphicsFormat.R32G32B32UInt: output = Format.FormatR32G32B32Uint; return true;
            case GraphicsFormat.R32G32B32Float: output = Format.FormatR32G32B32Float; return true;

            case GraphicsFormat.R11G11B10Float: output = Format.FormatR11G11B10Float; return true;

            case GraphicsFormat.B4G4R4A4UNorm: output = Format.FormatB4G4R4A4Unorm; return true;

            case GraphicsFormat.R8G8B8A8Int: output = Format.FormatR8G8B8A8Sint; return true;
            case GraphicsFormat.R8G8B8A8UInt: output = Format.FormatR8G8B8A8Uint; return true;
            case GraphicsFormat.R8G8B8A8Norm: output = Format.FormatR8G8B8A8SNorm; return true;
            case GraphicsFormat.R8G8B8A8UNorm: output = Format.FormatR8G8B8A8Unorm; return true;

            case GraphicsFormat.B8G8R8A8UNorm: output = Format.FormatB8G8R8A8Unorm; return true;

            case GraphicsFormat.R16G16B16A16Int: output = Format.FormatR16G16B16A16Sint; return true;
            case GraphicsFormat.R16G16B16A16UInt: output = Format.FormatR16G16B16A16Uint; return true;
            case GraphicsFormat.R16G16B16A16Norm: output = Format.FormatR16G16B16A16SNorm; return true;
            case GraphicsFormat.R16G16B16A16UNorm: output = Format.FormatR16G16B16A16Unorm; return true;

            case GraphicsFormat.R32G32B32A32Int: output = Format.FormatR32G32B32A32Sint; return true;
            case GraphicsFormat.R32G32B32A32UInt: output = Format.FormatR32G32B32A32Uint; return true;
            case GraphicsFormat.R32G32B32A32Float: output = Format.FormatR32G32B32A32Float; return true;

            case GraphicsFormat.R10G10B10A2UNorm: output = Format.FormatR10G10B10A2Unorm; return true;

            case GraphicsFormat.Alpha8: output = Format.FormatA8Unorm; return true;

            case GraphicsFormat.D16UNorm: output = Format.FormatD16Unorm; return true;
            case GraphicsFormat.D24UNormS8UInt: output = Format.FormatD24UnormS8Uint; return true;
            case GraphicsFormat.D32Float: output = Format.FormatD32Float; return true;
            case GraphicsFormat.D32FloatS8UInt: output = Format.FormatD32FloatS8X24Uint; return true;

            default:
#if DEBUG
                if (input.IsDefined()) throw new NotImplementedException($"Unimplemented case '{input}'.");
#endif
                output = default; return false;
        }
    }
    public static bool TryConvert(D3DShaderInputType input, D3DSrvDimension textureDimension, out ResourceType output) {
        switch (input) {
            case D3DShaderInputType.D3DSitCbuffer: output = ResourceType.ConstantBuffer; return true;
            case D3DShaderInputType.D3DSitSampler: output = ResourceType.Sampler; return true;
            case D3DShaderInputType.D3DSitTbuffer: output = ResourceType.TextureBuffer; return true;

            case D3DShaderInputType.D3DSitUavRwtyped: output = ResourceType.RWBuffer; return true;

            case D3DShaderInputType.D3DSitStructured: output = ResourceType.StructuredBuffer; return true;
            case D3DShaderInputType.D3DSitUavRwstructured: output = ResourceType.RWStructuredBuffer; return true;
            case D3DShaderInputType.D3DSitUavRwstructuredWithCounter: output = ResourceType.RWStructuredBufferWithCounter; return true;

            case D3DShaderInputType.D3DSitByteaddress: output = ResourceType.ByteAddressBuffer; return true;
            case D3DShaderInputType.D3DSitUavRwbyteaddress: output = ResourceType.RWByteAddressBuffer; return true;

            case D3DShaderInputType.D3DSitTexture:
                output = textureDimension switch {
                    D3DSrvDimension.D3DSrvDimensionTexture1D => ResourceType.Texture1D,
                    D3DSrvDimension.D3DSrvDimensionTexture1Darray => ResourceType.Texture1DArray,
                    D3DSrvDimension.D3DSrvDimensionTexture2D => ResourceType.Texture2D,
                    D3DSrvDimension.D3DSrvDimensionTexture2Darray => ResourceType.Texture2DArray,
                    D3DSrvDimension.D3DSrvDimensionTexture2Dms => ResourceType.Texture2DMS,
                    D3DSrvDimension.D3DSrvDimensionTexture2Dmsarray => ResourceType.Texture2DMSArray,
                    D3DSrvDimension.D3DSrvDimensionTexture3D => ResourceType.Texture3D,
                    D3DSrvDimension.D3DSrvDimensionTexturecube => ResourceType.TextureCube,
                    D3DSrvDimension.D3DSrvDimensionTexturecubearray => ResourceType.TextureCubeArray,

                    D3DSrvDimension.D3DSrvDimensionBuffer => ResourceType.Buffer,
                    D3DSrvDimension.D3DSrvDimensionBufferex => throw new NotImplementedException("BufferEx."),
                    D3DSrvDimension.D3DSrvDimensionUnknown or _ => throw new UnreachableException("Unknown/Undefined."),
                };
                return true;

            default: output = default; return false;
        }
    }
    public static D3D12ResourceStates Convert(ResourceStates input) {
        return
            (input.HasFlag(ResourceStates.ConstantBuffer) ? D3D12ResourceStates.VertexAndConstantBuffer : default) |
            (input.HasFlag(ResourceStates.IndexBuffer) ? D3D12ResourceStates.IndexBuffer : default) |
            (input.HasFlag(ResourceStates.ShaderResource) ? D3D12ResourceStates.AllShaderResource : default) |
            (input.HasFlag(ResourceStates.UnorderedAccess) ? D3D12ResourceStates.UnorderedAccess : default) |
            (input.HasFlag(ResourceStates.RenderTarget) ? D3D12ResourceStates.RenderTarget : default) |
            (input.HasFlag(ResourceStates.CopySource) ? D3D12ResourceStates.CopySource : default) |
            (input.HasFlag(ResourceStates.CopyDestination) ? D3D12ResourceStates.CopyDest : default) |
            (input.HasFlag(ResourceStates.DepthWrite) ? D3D12ResourceStates.DepthWrite : default) |
            (input.HasFlag(ResourceStates.DepthRead) ? D3D12ResourceStates.DepthRead : default) |
            (input.HasFlag(ResourceStates.Present) ? D3D12ResourceStates.Present : default)
            ;
    }

    public static bool TryConvert(TextureAddressingMode input, out TextureAddressMode output) {
        switch (input) {
            case TextureAddressingMode.Wrap: output = TextureAddressMode.Wrap; return true;
            case TextureAddressingMode.Mirror: output = TextureAddressMode.Mirror; return true;
            case TextureAddressingMode.Clamp: output = TextureAddressMode.Clamp; return true;
            case TextureAddressingMode.Border: output = TextureAddressMode.Border; return true;
            default: output = default; return false;
        }
    }
}