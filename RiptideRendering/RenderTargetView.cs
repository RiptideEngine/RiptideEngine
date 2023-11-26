namespace RiptideRendering;

[EnumExtension]
public enum RenderTargetViewDimension {
    Unknown = 0,
    
    Texture1D = 2,
    Texture1DArray = 3,
    Texture2D = 4,
    Texture2DArray = 5,
    Texture3D = 8,
}

[StructLayout(LayoutKind.Explicit)]
public struct RenderTargetViewDescription {
    [FieldOffset(0)] public RenderTargetViewDimension Dimension;
    [FieldOffset(4)] public GraphicsFormat Format;
    [FieldOffset(8)] public Texture1DView Texture1D;
    [FieldOffset(8)] public Texture1DArrayView Texture1DArray;
    [FieldOffset(8)] public Texture2DView Texture2D;
    [FieldOffset(8)] public Texture2DArrayView Texture2DArray;
    [FieldOffset(8)] public Texture3DView Texture3D;

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
        public uint PlaneSlice;
        public uint FirstArraySlice;
        public uint ArraySize;
    }
    public struct Texture3DView {
        public uint MipSlice;
        public uint FirstWSlice;
        public uint WSize;
    }
}

public abstract class RenderTargetView : RiptideRcObject;