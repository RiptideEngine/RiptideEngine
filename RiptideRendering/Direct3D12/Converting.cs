using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal static partial class Converting {
    public static bool TryConvert(TextureAddressingMode input, out TextureAddressMode output) {
        switch (input) {
            case TextureAddressingMode.Wrap: output = TextureAddressMode.Wrap; return true;
            case TextureAddressingMode.Mirror: output = TextureAddressMode.Mirror; return true;
            case TextureAddressingMode.Clamp: output = TextureAddressMode.Clamp; return true;
            case TextureAddressingMode.Border: output = TextureAddressMode.Border; return true;
            default: output = default; return false;
        }
    }

    public static bool TryConvert(ComparisonOperator input, out ComparisonFunc output) {
        switch (input) {
            case ComparisonOperator.Never: output = ComparisonFunc.Never; return true;
            case ComparisonOperator.Less: output = ComparisonFunc.Less; return true;
            case ComparisonOperator.Equal: output = ComparisonFunc.Equal; return true;
            case ComparisonOperator.LessEqual: output = ComparisonFunc.LessEqual; return true;
            case ComparisonOperator.Greater: output = ComparisonFunc.Greater; return true;
            case ComparisonOperator.NotEqual: output = ComparisonFunc.NotEqual; return true;
            case ComparisonOperator.GreaterEqual: output = ComparisonFunc.GreaterEqual; return true;
            case ComparisonOperator.Always: output = ComparisonFunc.Always; return true;
            default: output = default; return false;
        }
    }

    public static bool TryConvert(DescriptorTableType input, out DescriptorRangeType output) {
        switch (input) {
            case DescriptorTableType.ConstantBuffer: output = DescriptorRangeType.Cbv; return true;
            case DescriptorTableType.ShaderResourceView: output = DescriptorRangeType.Srv; return true;
            case DescriptorTableType.UnorderedAccessView: output = DescriptorRangeType.Uav; return true;
            case DescriptorTableType.Sampler: output = DescriptorRangeType.Sampler; return true;
            default: output = default; return false;
        }
    }
}