namespace RiptideRendering;

[EnumExtension]
public enum UnorderedAccessViewDimension {
    Unknown = 0,
    
    Buffer = 1,
    Texture1D = 2,
    Texture1DArray = 3,
    Texture2D = 4,
    Texture2DArray = 5,
    Texture3D = 6,
}

[StructLayout(LayoutKind.Explicit)]
public struct UnorderedAccessViewDescription {
    [FieldOffset(0)] public UnorderedAccessViewDimension Dimension;
    [FieldOffset(4)] public GraphicsFormat Format;
    [FieldOffset(8)] public BufferView Buffer;
    [FieldOffset(8)] public Texture1DView Texture1D;
    [FieldOffset(8)] public Texture1DArrayView Texture1DArray;
    [FieldOffset(8)] public Texture2DView Texture2D;
    [FieldOffset(8)] public Texture2DArrayView Texture2DArray;
    [FieldOffset(8)] public Texture3DView Texture3D;
    
    public struct BufferView {
        public uint FirstElement;
        public uint NumElements;
        public uint StructureSize;
        public bool IsRaw;
    }
    public struct Texture1DView {
        public uint MipSlice;
    }
    public struct Texture1DArrayView {
        public uint MipSlice;
        public uint FirstArraySlice;
        public uint ArraySize;
    }
    public struct Texture2DView {
        public uint MipSlice;
        public uint PlaneSlice;
    }
    public struct Texture2DArrayView {
        public uint MipSlice;
        public uint FirstArraySlice;
        public uint ArraySize;
        public uint PlaneSlice;
    }
    public struct Texture3DView {
        public uint MipSlice;
        public uint FirstWSlice;
        public uint WSize;
    }
}

public abstract class UnorderedAccessView : RenderingObject;