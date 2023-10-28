namespace RiptideRendering;

[EnumExtension]
public enum ResourceType {
    Unknown = 0,

    Sampler,

    ConstantBuffer,
    TextureBuffer,

    Buffer,
    RWBuffer,

    StructuredBuffer,
    RWStructuredBuffer,
    RWStructuredBufferWithCounter,

    AppendStructuredBuffer,
    ConsumeStructuredBuffer,

    ByteAddressBuffer,
    RWByteAddressBuffer,

    Texture1D,
    RWTexture1D,
    Texture1DArray,
    RWTexture1DArray,

    Texture2D,
    RWTexture2D,
    Texture2DArray,
    RWTexture2DArray,
    Texture2DMS,
    Texture2DMSArray,

    Texture3D,
    RWTexture3D,

    TextureCube,
    TextureCubeArray,
}

public static partial class ResourceTypeExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsReadWrite(this ResourceType type) => type is ResourceType.RWBuffer or ResourceType.RWStructuredBuffer or ResourceType.RWStructuredBufferWithCounter or ResourceType.AppendStructuredBuffer or ResourceType.ConsumeStructuredBuffer or ResourceType.RWByteAddressBuffer or ResourceType.RWTexture1D or ResourceType.RWTexture1DArray or ResourceType.RWTexture2D or ResourceType.RWTexture2DArray or ResourceType.RWTexture3D;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsReadonly(this ResourceType type) => type is ResourceType.ConstantBuffer or ResourceType.TextureBuffer or ResourceType.Buffer or ResourceType.StructuredBuffer or ResourceType.ByteAddressBuffer or ResourceType.Texture1D or ResourceType.Texture1DArray or ResourceType.Texture2D or ResourceType.Texture2DArray or ResourceType.Texture2DMS or ResourceType.Texture2DMSArray or ResourceType.Texture3D or ResourceType.TextureCube or ResourceType.TextureCubeArray;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTexture(this ResourceType type) => type is >= ResourceType.Texture1D and <= ResourceType.TextureCubeArray;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBuffer(this ResourceType type) => type is >= ResourceType.ConstantBuffer and <= ResourceType.RWByteAddressBuffer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsReadonlyBuffer(this ResourceType type) => type is ResourceType.ConstantBuffer or ResourceType.TextureBuffer or ResourceType.Buffer or ResourceType.StructuredBuffer or ResourceType.ByteAddressBuffer;
}