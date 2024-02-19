namespace Riptide.ShaderCompilation;

public enum ReadonlyResourceType : ushort {
    Unknown = 0,
    
    Texture1D,
    Texture1DArray,
    Texture2D,
    Texture2DArray,
    Texture2DMS,
    Texture2DMSArray,
    Texture3D,
    TextureCube,
    TextureCubeArray,
        
    Buffer,
    StructuredBuffer, 
    ByteAddressBuffer,
}

public static class ShaderResourceTypeExtensions {
    public static bool IsBuffer(this ReadonlyResourceType type) => type is ReadonlyResourceType.Buffer or ReadonlyResourceType.StructuredBuffer or ReadonlyResourceType.ByteAddressBuffer;
    public static bool IsTexture(this ReadonlyResourceType type) => type is >= ReadonlyResourceType.Texture1D and <= ReadonlyResourceType.TextureCubeArray;
}