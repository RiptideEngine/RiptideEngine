namespace RiptideRendering;

[EnumExtension]
public enum DepthStencilViewDimension {
    Unknown = 0,
    
    Texture1D = 1,
    Texture1DArray = 2,
    Texture2D = 3,
    Texture2DArray = 4,
}

[StructLayout(LayoutKind.Explicit)]
public struct DepthStencilViewDescription {
    [FieldOffset(0)] public DepthStencilViewDimension Dimension;
    [FieldOffset(4)] public GraphicsFormat Format;
    [FieldOffset(8)] public Texture1DView Texture1D;
    [FieldOffset(8)] public Texture1DArrayView Texture1DArray;
    [FieldOffset(8)] public Texture2DView Texture2D;
    [FieldOffset(8)] public Texture2DArrayView Texture2DArray;

    public struct Texture1DView {
        public uint MipSlice;
    }
    public struct Texture1DArrayView {
        public uint MipSlice;
        public uint ArraySize;
        public uint FirstArraySlice;
    }
    public struct Texture2DView {
        public uint MipSlice;
    }
    public struct Texture2DArrayView {
        public uint MipSlice;
        public uint ArraySize;
        public uint FirstArraySlice;
    }
}

public abstract class DepthStencilView : RenderingObject;