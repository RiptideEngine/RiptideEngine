namespace RiptideRendering;

[EnumExtension]
public enum ShaderResourceViewDimension {
    Unknown = 0,
    
    Buffer = 1,
    Texture1D = 2,
    Texture1DArray = 3,
    Texture2D = 4,
    Texture2DArray = 5,
    //Texture2DMS = 6,
    //Texture2DMSArray = 7,
    Texture3D = 8,
    TextureCube = 9,
    TextureCubeArray = 10,
}

[StructLayout(LayoutKind.Explicit)]
public struct ShaderResourceViewDescription {
    [FieldOffset(0)] public ShaderResourceViewDimension Dimension;
    [FieldOffset(4)] public GraphicsFormat Format;
    [FieldOffset(8)] public BufferView Buffer;
    [FieldOffset(8)] public Texture1DView Texture1D;
    [FieldOffset(8)] public Texture1DArrayView Texture1DArray;
    [FieldOffset(8)] public Texture2DView Texture2D;
    [FieldOffset(8)] public Texture2DArrayView Texture2DArray;
    [FieldOffset(8)] public Texture3DView Texture3D;
    [FieldOffset(8)] public TextureCubeView TextureCube;
    [FieldOffset(8)] public TextureCubeArrayView TextureCubeArray;
    
    public struct BufferView {
        public uint FirstElement;
        public uint NumElements;
        public uint StructureSize;
        public bool IsRaw;
    }
    public struct Texture1DView {
        public uint MipLevels;
        public uint MostDetailedMip;
    }
    public struct Texture1DArrayView {
        public uint MipLevels;
        public uint MostDetailedMip;
        public uint FirstArraySlice;
        public uint ArraySize;
    }
    public struct Texture2DView {
        public uint MipLevels;
        public uint MostDetailedMip;
        public uint PlaneSlice;
    }
    public struct Texture2DArrayView {
        public uint MipLevels;
        public uint MostDetailedMip;
        public uint FirstArraySlice;
        public uint ArraySize;
        public uint PlaneSlice;
    }
    public struct Texture3DView {
        public uint MipLevels;
        public uint MostDetailedMip;
    }
    public struct TextureCubeView {
        public uint MipLevels;
        public uint MostDetailedMip;
    }
    public struct TextureCubeArrayView {
        public uint MipLevels;
        public uint MostDetailedMip;
        public uint NumCubes;
        public uint First2DArrayFace;
    }
}

public abstract class ShaderResourceView : RenderingObject;