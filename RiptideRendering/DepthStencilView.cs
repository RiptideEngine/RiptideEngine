namespace RiptideRendering;

public readonly record struct NativeDepthStencilView(ulong Handle);

public enum DepthStencilViewDimension {
    Texture1D,
    Texture1DArray,
    Texture2D,
    Texture2DArray,
    Texture2DMS,
    Texture2DMSArray,
}

[StructLayout(LayoutKind.Explicit)]
public struct DepthStencilViewDescriptor {
    [FieldOffset(0)] public DepthStencilViewDimension Dimension;
    [FieldOffset(4)] public GraphicsFormat Format;

    [FieldOffset(8)] public Texture1DView Texture1D;
    [FieldOffset(8)] public Texture1DArrayView Texture1DArray;
    [FieldOffset(8)] public Texture2DView Texture2D;
    [FieldOffset(8)] public Texture2DArrayView Texture2DArray;
    [FieldOffset(8)] public Texture2DMSView Texture2DMS;
    [FieldOffset(8)] public Texture2DMSArrayView Texture2DMSArray;

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
    }
    public struct Texture2DArrayView {
        public uint MipSlice;
        public uint FirstArraySlice;
        public uint ArraySize;
    }
    public struct Texture2DMSView { }
    public struct Texture2DMSArrayView {
        public uint FirstArraySlice;
        public uint ArraySize;
    }
}

public abstract class DepthStencilView : RenderingObject {
    public NativeDepthStencilView NativeView { get; protected set; }

    public static implicit operator NativeDepthStencilView(DepthStencilView view) => view.NativeView;
}