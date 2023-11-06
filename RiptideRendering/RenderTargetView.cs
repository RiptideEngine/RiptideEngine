namespace RiptideRendering;

public readonly record struct NativeRenderTargetView(ulong Handle);

public enum RenderTargetViewDimension {
    Buffer,
    Texture1D,
    Texture1DArray,
    Texture2D,
    Texture2DArray,
    Texture2DMS,
    Texture2DMSArray,
    Texture3D,
}

[StructLayout(LayoutKind.Explicit)]
public struct RenderTargetViewDescriptor {
    [FieldOffset(0)] public RenderTargetViewDimension Dimension;
    [FieldOffset(4)] public GraphicsFormat Format;

    [FieldOffset(8)] public BufferView Buffer;
    [FieldOffset(8)] public Texture1DView Texture1D;
    [FieldOffset(8)] public Texture1DArrayView Texture1DArray;
    [FieldOffset(8)] public Texture2DView Texture2D;
    [FieldOffset(8)] public Texture2DArrayView Texture2DArray;
    [FieldOffset(8)] public Texture2DMSView Texture2DMS;
    [FieldOffset(8)] public Texture2DMSArrayView Texture2DMSArray;
    [FieldOffset(8)] public Texture3DView Texture3D;

    public struct BufferView {
        public uint FirstElement, NumElements;
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
        public uint MipSlice, PlaneSlice;
    }
    public struct Texture2DArrayView {
        public uint MipSlice, PlaneSlice;
        public uint FirstArraySlice;
        public uint ArraySize;
    }
    public struct Texture2DMSView { }
    public struct Texture2DMSArrayView {
        public uint FirstArraySlice;
        public uint ArraySize;
    }
    public struct Texture3DView {
        public uint MipSlice;
        public uint FirstWSlice, WSize;
    }
}

public abstract class RenderTargetView : RenderingObject {
    public NativeRenderTargetView NativeView { get; protected set; }

    public static implicit operator NativeRenderTargetView(RenderTargetView view) => view.NativeView;
}