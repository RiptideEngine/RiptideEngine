namespace RiptideRendering;

[EnumExtension, Flags]
public enum SignatureFlags {
    None = 0,
}

public enum ResourceParameterType {
    Constants = 0,
    Descriptors = 1,
}

[EnumExtension]
public enum DescriptorTableType {
    ConstantBuffer,
    ShaderResourceView,
    UnorderedAccessView,
    Sampler,
}

public struct ResourceParameter {
    public ResourceParameterType Type;
    public ConstantParameter Constants;
    public DescriptorParameter Descriptors;

    public static ResourceParameter CreateConstants(uint numConstants, uint register, uint space) => new() {
        Type = ResourceParameterType.Constants,
        Constants = new() {
            NumConstants = numConstants,
            Register = register,
            Space = space,
        },
    };
    public static ResourceParameter CreateDescriptors(DescriptorTableType type, uint numDescriptors, uint baseRegister, uint space) => new() {
        Type = ResourceParameterType.Descriptors,
        Descriptors = new() {
            Type = type,
            NumDescriptors = numDescriptors,
            BaseRegister = baseRegister,
            Space = space,
        },
    };

    public struct ConstantParameter {
        public uint Register;
        public uint Space;
        public uint NumConstants;
    }

    public struct DescriptorParameter {
        public DescriptorTableType Type;
        public uint BaseRegister;
        public uint Space;
        public uint NumDescriptors;
    }
}

public struct ImmutableSamplerDescription {
    public uint Register, Space;

    public SamplerFilter Filter;
    public TextureAddressingMode AddressU, AddressV, AddressW;
    public float MipLodBias;
    public uint MaxAnisotropy;
    public ComparisonOperator ComparisonOp;
    public float MinLod, MaxLod;
}

public struct ResourceSignatureDescription {
    public SignatureFlags Flags;
    
    public ResourceParameter[] Parameters;
    public ImmutableSamplerDescription[] ImmutableSamplers;
}

public abstract class ResourceSignature : RenderingObject {
    public abstract ReadOnlySpan<ResourceParameter> Parameters { get; }
}