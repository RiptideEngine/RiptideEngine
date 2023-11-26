namespace RiptideRendering;

[EnumExtension, Flags]
public enum SignatureFlags {
    None = 0,
}

public enum ResourceParameterType {
    Constants = 0,
    Table = 1,
}

[EnumExtension]
public enum ResourceRangeType {
    ConstantBuffer = 0,
    ShaderResourceView = 1,
    UnorderedAccess = 2,
    Sampler = 3,
}

public struct ResourceRange {
    public ResourceRangeType Type;
    public uint BaseRegister;
    public uint Space;
    public uint NumResources;
}

public struct ResourceParameter {
    public ResourceParameterType Type;
    public ConstantParameter Constants;
    public TableParameter Table;

    public struct ConstantParameter {
        public uint Register;
        public uint Space;
        public uint NumConstants;
    }

    public struct TableParameter {
        public ResourceRange[] Ranges;
    }
}

public struct ImmutableSamplerDescriptor {
    public uint Register, Space;

    public SamplerFilter Filter;
    public TextureAddressingMode AddressU, AddressV, AddressW;
    public float MipLodBias;
    public uint MaxAnisotropy;
    public ComparisonOperator ComparisonOp;
    public float MinLod, MaxLod;
}

public struct ResourceSignatureDescriptor {
    public SignatureFlags Flags;

    public ResourceParameter[] Parameters;
    public ImmutableSamplerDescriptor[] ImmutableSamplers;
}

public abstract class ResourceSignature : RenderingObject { }