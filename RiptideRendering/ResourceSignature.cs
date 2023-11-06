namespace RiptideRendering;

public enum ResourceRangeType {
    ConstantBuffer = 0,
    ReadonlyResource = 1,
    UnorderedAccess = 2,
    Sampler = 3,
}

public struct ResourceRange {
    public ResourceRangeType Type;
    public uint BaseRegister, Space, NumResources;
}

public struct ResourceTableDescriptor {
    public ResourceRange[] Table;
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

public abstract class ResourceSignature : RenderingObject { }