namespace RiptideRendering;

public readonly record struct NativeResourceView(ulong Handle) {
    public static implicit operator ulong(NativeResourceView view) => view.Handle;
}

[EnumExtension]
public enum ResourceViewDimension {
    Buffer,
    Texture1D,
    Texture1DArray,
    Texture2D,
    Texture2DArray,
    Texture2DMS,
    Texture2DMSArray,
    Texture3D,
    TextureCube,
    TextureCubeArray,
}

[StructLayout(LayoutKind.Explicit)]
public struct ResourceViewDescriptor {
    [FieldOffset(0)] public ResourceViewDimension Dimension;
    [FieldOffset(4)] public GraphicsFormat Format;

    [FieldOffset(8)] public BufferView Buffer;
    [FieldOffset(8)] public Texture1DView Texture1D;
    [FieldOffset(8)] public Texture1DArrayView Texture1DArray;
    [FieldOffset(8)] public Texture2DView Texture2D;
    [FieldOffset(8)] public Texture2DArrayView Texture2DArray;
    [FieldOffset(8)] public Texture2DMSView Texture2DMS;
    [FieldOffset(8)] public Texture2DMSArrayView Texture2DMSArray;
    [FieldOffset(8)] public Texture3DView Texture3D;
    [FieldOffset(8)] public TextureCubeView TextureCube;
    [FieldOffset(8)] public TextureCubeArrayView TextureCubeArray;

    public struct BufferView {
        public uint FirstElement;
        public uint NumElements;
        public uint StructureSize;
        public bool IsRawBuffer;
    }
    public struct Texture1DView {
        public uint MostDetailedMip;
        public uint MipLevels;
    }
    public struct Texture1DArrayView {
        public uint MostDetailedMip;
        public uint MipLevels;
        public uint FirstArraySlice;
        public uint ArraySize;
    }
    public struct Texture2DView {
        public uint MostDetailedMip;
        public uint MipLevels;
        public uint PlaneSlice;
    }
    public struct Texture2DArrayView {
        public uint MostDetailedMip;
        public uint MipLevels;
        public uint FirstArraySlice;
        public uint ArraySize;
        public uint PlaneSlice;
    }
    public struct Texture2DMSView { }
    public struct Texture2DMSArrayView {
        public uint FirstArraySlice;
        public uint ArraySize;
    }
    public struct Texture3DView {
        public uint MostDetailedMip;
        public uint MipLevels;
    }
    public struct TextureCubeView {
        public uint MostDetailedMip;
        public uint MipLevels;
    }
    public struct TextureCubeArrayView {
        public uint MostDetailedMip;
        public uint MipLevels;
        public uint NumCubes;
        public uint First2DArrayFace;
    }
}

public abstract class ResourceView : RenderingObject {
    public NativeResourceView NativeView { get; protected set; }

    public static implicit operator NativeResourceView(ResourceView view) => view.NativeView;
}