namespace RiptideRendering;

[EnumExtension]
public enum GraphicsFormat {
    Unknown = 0,

    R8Int, R8UInt, R8Norm, R8UNorm,
    R16Int, R16UInt, R16Norm, R16UNorm,
    R32Int, R32UInt, R32Float,

    R8G8Int, R8G8UInt, R8G8Norm, R8G8UNorm,
    R16G16Int, R16G16UInt, R16G16Norm, R16G16UNorm,
    R32G32Int, R32G32UInt, R32G32Float,

    B5G6R5UNorm, R11G11B10Float,

    R32G32B32Int, R32G32B32UInt, R32G32B32Float,

    R8G8B8A8Int, R8G8B8A8UInt, R8G8B8A8Norm, R8G8B8A8UNorm,
    R16G16B16A16Int, R16G16B16A16UInt, R16G16B16A16Norm, R16G16B16A16UNorm,
    R32G32B32A32Int, R32G32B32A32UInt, R32G32B32A32Float,

    B8G8R8A8UNorm,
    B4G4R4A4UNorm, B5G5R5A1UNorm, R10G10B10A2UNorm,

    Alpha8,

    D16UNorm, D24UNormS8UInt, D32Float, D32FloatS8UInt,
}

public static partial class GraphicsFormatExtensions {
    /// <summary>
    /// Return the size in byte of each pixel using the given format.
    /// </summary>
    /// <param name="format">Pixel color format.</param>
    /// <param name="stride">Output stride.</param>
    /// <returns>Whether the format is valid to be used to get stride.</returns>
    public static bool TryGetStride(this GraphicsFormat format, out uint stride) {
        switch (format) {
            case GraphicsFormat.R8Int or GraphicsFormat.R8UInt or GraphicsFormat.R8Norm or GraphicsFormat.R8UNorm: stride = 1; return true;
            case GraphicsFormat.R16Int or GraphicsFormat.R16UInt or GraphicsFormat.R16Norm or GraphicsFormat.R16UNorm: stride = 2; return true;
            case GraphicsFormat.R32Int or GraphicsFormat.R32UInt or GraphicsFormat.R32Float: stride = 4; return true;

            case GraphicsFormat.R8G8Int or GraphicsFormat.R8G8UInt or GraphicsFormat.R8G8Norm or GraphicsFormat.R8G8UNorm: stride = 2; return true;
            case GraphicsFormat.R16G16Int or GraphicsFormat.R16G16UInt or GraphicsFormat.R16G16Norm or GraphicsFormat.R16G16UNorm: stride = 4; return true;
            case GraphicsFormat.R32G32Int or GraphicsFormat.R32G32UInt or GraphicsFormat.R32G32Float: stride = 8; return true;

            case GraphicsFormat.R32G32B32Int or GraphicsFormat.R32G32B32UInt or GraphicsFormat.R32G32B32Float: stride = 12; return true;

            case GraphicsFormat.R11G11B10Float: stride = 4; return true;

            case GraphicsFormat.B4G4R4A4UNorm or GraphicsFormat.B5G6R5UNorm or GraphicsFormat.B5G5R5A1UNorm: stride = 2; return true;

            case GraphicsFormat.R8G8B8A8Int or GraphicsFormat.R8G8B8A8UInt or GraphicsFormat.R8G8B8A8Norm or GraphicsFormat.R8G8B8A8UNorm or GraphicsFormat.B8G8R8A8UNorm: stride = 4; return true;
            case GraphicsFormat.R16G16B16A16Int or GraphicsFormat.R16G16B16A16UInt or GraphicsFormat.R16G16B16A16Norm or GraphicsFormat.R16G16B16A16UNorm: stride = 8; return true;
            case GraphicsFormat.R32G32B32A32Int or GraphicsFormat.R32G32B32A32UInt or GraphicsFormat.R32G32B32A32Float: stride = 16; return true;

            case GraphicsFormat.R10G10B10A2UNorm: stride = 4; return true;

            case GraphicsFormat.Alpha8: stride = 1; return true;

            case GraphicsFormat.D16UNorm: stride = 2; return true;
            case GraphicsFormat.D24UNormS8UInt or GraphicsFormat.D32Float: stride = 4; return true;
            case GraphicsFormat.D32FloatS8UInt: stride = 8; return true;

            default:
#if DEBUG
                if (format.IsDefined()) throw new NotImplementedException($"Unimplemented case '{format}'.");
#endif
                stride = 0; return false;
        }
    }
}