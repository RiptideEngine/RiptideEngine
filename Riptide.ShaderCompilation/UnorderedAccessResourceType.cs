namespace Riptide.ShaderCompilation;

public enum UnorderedAccessResourceType {
    Unknown = 0,
    
    RWTexture1D,
    RWTexture1DArray,
    RWTexture2D,
    RWTexture2DArray,
    RWTexture3D,
        
    RWBuffer,
    RWStructuredBuffer,
    RWStructuredBufferWithCounter,
    RWByteAddressBuffer,
    AppendStructuredBuffer,
    ConsumeStructuredBuffer,
}