using Silk.NET.DXGI;

namespace RiptideRendering.Direct3D12;

partial class Converting {
    public static bool TryConvert(GraphicsFormat format, out Format output) {
        switch (format) {
            case GraphicsFormat.Unknown: output = Format.FormatUnknown; return true;
            
            case GraphicsFormat.R8Int: output = Format.FormatR8Sint; return true;
            case GraphicsFormat.R8UInt: output = Format.FormatR8Uint; return true;
            case GraphicsFormat.R8Norm: output = Format.FormatR8SNorm; return true;
            case GraphicsFormat.R8UNorm: output = Format.FormatR8Unorm; return true;
            case GraphicsFormat.R8Typeless: output = Format.FormatR8Typeless; return true;
            
            case GraphicsFormat.R16Int: output = Format.FormatR16Sint; return true;
            case GraphicsFormat.R16UInt: output = Format.FormatR16Uint; return true;
            case GraphicsFormat.R16Norm: output = Format.FormatR16SNorm; return true;
            case GraphicsFormat.R16UNorm: output = Format.FormatR16Unorm; return true;
            case GraphicsFormat.R16Typeless: output = Format.FormatR16Typeless; return true;
            
            case GraphicsFormat.R32Int: output = Format.FormatR32Sint; return true;
            case GraphicsFormat.R32UInt: output = Format.FormatR32Uint; return true;
            case GraphicsFormat.R32Float: output = Format.FormatR32Float; return true;
            case GraphicsFormat.R32Typeless: output = Format.FormatR32Typeless; return true;

            case GraphicsFormat.R8G8Int: output = Format.FormatR8G8Sint; return true;
            case GraphicsFormat.R8G8UInt: output = Format.FormatR8G8Uint; return true;
            case GraphicsFormat.R8G8Norm: output = Format.FormatR8G8SNorm; return true;
            case GraphicsFormat.R8G8UNorm: output = Format.FormatR8G8Unorm; return true;
            case GraphicsFormat.R8G8Typeless: output = Format.FormatR8G8Typeless; return true;
            case GraphicsFormat.R16G16Int: output = Format.FormatR16G16Sint; return true;
            case GraphicsFormat.R16G16UInt: output = Format.FormatR16G16Uint; return true;
            case GraphicsFormat.R16G16Norm: output = Format.FormatR16G16SNorm; return true;
            case GraphicsFormat.R16G16UNorm: output = Format.FormatR16G16Unorm; return true;
            case GraphicsFormat.R16G16Typeless: output = Format.FormatR16G16Typeless; return true;
            case GraphicsFormat.R32G32Int: output = Format.FormatR32G32Sint; return true;
            case GraphicsFormat.R32G32UInt: output = Format.FormatR32G32Uint; return true;
            case GraphicsFormat.R32G32Float: output = Format.FormatR32G32Float; return true;
            case GraphicsFormat.R32G32Typeless: output = Format.FormatR32G32Typeless; return true;

            case GraphicsFormat.B5G6R5UNorm: output = Format.FormatB5G6R5Unorm; return true;
            case GraphicsFormat.B5G5R5A1UNorm: output = Format.FormatB5G5R5A1Unorm; return true;
            case GraphicsFormat.R11G11B10Float: output = Format.FormatR11G11B10Float; return true;

            case GraphicsFormat.R32G32B32Int: output = Format.FormatR32G32B32Sint; return true;
            case GraphicsFormat.R32G32B32UInt: output = Format.FormatR32G32B32Uint; return true;
            case GraphicsFormat.R32G32B32Float: output = Format.FormatR32G32B32Float; return true;
            case GraphicsFormat.R32G32B32Typeless: output = Format.FormatR32G32B32Typeless; return true;

            case GraphicsFormat.R8G8B8A8Int: output = Format.FormatR8G8B8A8Sint; return true;
            case GraphicsFormat.R8G8B8A8UInt: output = Format.FormatR8G8B8A8Uint; return true;
            case GraphicsFormat.R8G8B8A8Norm: output = Format.FormatR8G8B8A8SNorm; return true;
            case GraphicsFormat.R8G8B8A8UNorm: output = Format.FormatR8G8B8A8Unorm; return true;
            case GraphicsFormat.R8G8B8A8Typeless: output = Format.FormatR8G8B8A8Typeless; return true;
            case GraphicsFormat.R16G16B16A16Int: output = Format.FormatR16G16B16A16Sint; return true;
            case GraphicsFormat.R16G16B16A16UInt: output = Format.FormatR16G16B16A16Uint; return true;
            case GraphicsFormat.R16G16B16A16Norm: output = Format.FormatR16G16B16A16SNorm; return true;
            case GraphicsFormat.R16G16B16A16UNorm: output = Format.FormatR16G16B16A16Unorm; return true;
            case GraphicsFormat.R16G16B16A16Typeless: output = Format.FormatR16G16B16A16Typeless; return true;
            case GraphicsFormat.R32G32B32A32Int: output = Format.FormatR32G32B32A32Sint; return true;
            case GraphicsFormat.R32G32B32A32UInt: output = Format.FormatR32G32B32A32Uint; return true;
            case GraphicsFormat.R32G32B32A32Float: output = Format.FormatR32G32B32A32Float; return true;
            case GraphicsFormat.R32G32B32A32Typeless: output = Format.FormatR32G32B32A32Typeless; return true;

            case GraphicsFormat.B8G8R8A8UNorm: output = Format.FormatB8G8R8A8Unorm; return true;
            case GraphicsFormat.B8G8R8A8Typeless: output = Format.FormatB8G8R8A8Typeless; return true;
            case GraphicsFormat.B8G8R8X8Typeless: output = Format.FormatB8G8R8X8Typeless; return true;
            case GraphicsFormat.B4G4R4A4UNorm: output = Format.FormatB4G4R4A4Unorm; return true;
            case GraphicsFormat.R10G10B10A2UNorm: output = Format.FormatR10G10B10A2Unorm; return true;
            case GraphicsFormat.R10G10B10A2Typeless: output = Format.FormatR10G10B10A2Typeless; return true;

            case GraphicsFormat.A8UNorm: output = Format.FormatA8Unorm; return true;

            case GraphicsFormat.D16UNorm: output = Format.FormatD16Unorm; return true;
            case GraphicsFormat.D24UNormS8UInt: output = Format.FormatD24UnormS8Uint; return true;
            case GraphicsFormat.R24UNormX8Typeless: output = Format.FormatR24UnormX8Typeless; return true;
            case GraphicsFormat.R24G8Typeless: output = Format.FormatR24G8Typeless; return true;
            case GraphicsFormat.X24TypelessG8UInt: output = Format.FormatX24TypelessG8Uint; return true;
            case GraphicsFormat.D32Float: output = Format.FormatD32Float; return true;
            case GraphicsFormat.D32FloatS8X24UInt: output = Format.FormatD32FloatS8X24Uint; return true;
            case GraphicsFormat.R32FloatX8X24Typeless: output = Format.FormatR32FloatX8X24Typeless; return true;
            case GraphicsFormat.X32TypelessG8X24Uint: output = Format.FormatX32TypelessG8X24Uint; return true;
            case GraphicsFormat.R32G8X24Typeless: output = Format.FormatR32G8X24Typeless; return true;
            
            default: output = default; return false;
        }
    }
    public static bool TryConvert(Format format, out GraphicsFormat output) {
        switch (format) {
            case Format.FormatUnknown: output = GraphicsFormat.Unknown; return true;
            
            case Format.FormatR8Sint: output = GraphicsFormat.R8Int; return true;
            case Format.FormatR8Uint: output = GraphicsFormat.R8UInt; return true;
            case Format.FormatR8SNorm: output = GraphicsFormat.R8Norm; return true;
            case Format.FormatR8Unorm: output = GraphicsFormat.R8UNorm; return true;
            case Format.FormatR8Typeless: output = GraphicsFormat.R8Typeless; return true;
            
            case Format.FormatR16Sint: output = GraphicsFormat.R16Int; return true;
            case Format.FormatR16Uint: output = GraphicsFormat.R16UInt; return true;
            case Format.FormatR16SNorm: output = GraphicsFormat.R16Norm; return true;
            case Format.FormatR16Unorm: output = GraphicsFormat.R16UNorm; return true;
            case Format.FormatR16Typeless: output = GraphicsFormat.R16Typeless; return true;
            
            case Format.FormatR32Sint: output = GraphicsFormat.R32Int; return true;
            case Format.FormatR32Uint: output = GraphicsFormat.R32UInt; return true;
            case Format.FormatR32Float: output = GraphicsFormat.R32Float; return true;
            case Format.FormatR32Typeless: output = GraphicsFormat.R32Typeless; return true;

            case Format.FormatR8G8Sint: output = GraphicsFormat.R8G8Int; return true;
            case Format.FormatR8G8Uint: output = GraphicsFormat.R8G8UInt; return true;
            case Format.FormatR8G8SNorm: output = GraphicsFormat.R8G8Norm; return true;
            case Format.FormatR8G8Unorm: output = GraphicsFormat.R8G8UNorm; return true;
            case Format.FormatR8G8Typeless: output = GraphicsFormat.R8G8Typeless; return true;
            case Format.FormatR16G16Sint: output = GraphicsFormat.R16G16Int; return true;
            case Format.FormatR16G16Uint: output = GraphicsFormat.R16G16UInt; return true;
            case Format.FormatR16G16SNorm: output = GraphicsFormat.R16G16Norm; return true;
            case Format.FormatR16G16Unorm: output = GraphicsFormat.R16G16UNorm; return true;
            case Format.FormatR16G16Typeless: output = GraphicsFormat.R16G16Typeless; return true;
            case Format.FormatR32G32Sint: output = GraphicsFormat.R32G32Int; return true;
            case Format.FormatR32G32Uint: output = GraphicsFormat.R32G32UInt; return true;
            case Format.FormatR32G32Float: output = GraphicsFormat.R32G32Float; return true;
            case Format.FormatR32G32Typeless: output = GraphicsFormat.R32G32Typeless; return true;

            case Format.FormatB5G6R5Unorm: output = GraphicsFormat.B5G6R5UNorm; return true;
            case Format.FormatB5G5R5A1Unorm: output = GraphicsFormat.B5G5R5A1UNorm; return true;
            case Format.FormatR11G11B10Float: output = GraphicsFormat.R11G11B10Float; return true;

            case Format.FormatR32G32B32Sint: output = GraphicsFormat.R32G32B32Int; return true;
            case Format.FormatR32G32B32Uint: output = GraphicsFormat.R32G32B32UInt; return true;
            case Format.FormatR32G32B32Float: output = GraphicsFormat.R32G32B32Float; return true;
            case Format.FormatR32G32B32Typeless: output = GraphicsFormat.R32G32B32Typeless; return true;

            case Format.FormatR8G8B8A8Sint: output = GraphicsFormat.R8G8B8A8Int; return true;
            case Format.FormatR8G8B8A8Uint: output = GraphicsFormat.R8G8B8A8UInt; return true;
            case Format.FormatR8G8B8A8SNorm: output = GraphicsFormat.R8G8B8A8Norm; return true;
            case Format.FormatR8G8B8A8Unorm: output = GraphicsFormat.R8G8B8A8UNorm; return true;
            case Format.FormatR8G8B8A8Typeless: output = GraphicsFormat.R8G8B8A8Typeless; return true;
            case Format.FormatR16G16B16A16Sint: output = GraphicsFormat.R16G16B16A16Int; return true;
            case Format.FormatR16G16B16A16Uint: output = GraphicsFormat.R16G16B16A16UInt; return true;
            case Format.FormatR16G16B16A16SNorm: output = GraphicsFormat.R16G16B16A16Norm; return true;
            case Format.FormatR16G16B16A16Unorm: output = GraphicsFormat.R16G16B16A16UNorm; return true;
            case Format.FormatR16G16B16A16Typeless: output = GraphicsFormat.R16G16B16A16Typeless; return true;
            case Format.FormatR32G32B32A32Sint: output = GraphicsFormat.R32G32B32A32Int; return true;
            case Format.FormatR32G32B32A32Uint: output = GraphicsFormat.R32G32B32A32UInt; return true;
            case Format.FormatR32G32B32A32Float: output = GraphicsFormat.R32G32B32A32Float; return true;
            case Format.FormatR32G32B32A32Typeless: output = GraphicsFormat.R32G32B32A32Typeless; return true;

            case Format.FormatB8G8R8A8Unorm: output = GraphicsFormat.B8G8R8A8UNorm; return true;
            case Format.FormatB8G8R8A8Typeless: output = GraphicsFormat.B8G8R8A8Typeless; return true;
            case Format.FormatB8G8R8X8Typeless: output = GraphicsFormat.B8G8R8X8Typeless; return true;
            case Format.FormatB4G4R4A4Unorm: output = GraphicsFormat.B4G4R4A4UNorm; return true;
            case Format.FormatR10G10B10A2Unorm: output = GraphicsFormat.R10G10B10A2UNorm; return true;
            case Format.FormatR10G10B10A2Typeless: output = GraphicsFormat.R10G10B10A2Typeless; return true;

            case Format.FormatA8Unorm: output = GraphicsFormat.A8UNorm; return true;

            case Format.FormatD16Unorm: output = GraphicsFormat.D16UNorm; return true;
            case Format.FormatD24UnormS8Uint: output = GraphicsFormat.D24UNormS8UInt; return true;
            case Format.FormatR24UnormX8Typeless: output = GraphicsFormat.R24UNormX8Typeless; return true;
            case Format.FormatR24G8Typeless: output = GraphicsFormat.R24G8Typeless; return true;
            case Format.FormatX24TypelessG8Uint: output = GraphicsFormat.X24TypelessG8UInt; return true;
            case Format.FormatD32Float: output = GraphicsFormat.D32Float; return true;
            case Format.FormatD32FloatS8X24Uint: output = GraphicsFormat.D32FloatS8X24UInt; return true;
            case Format.FormatR32FloatX8X24Typeless: output = GraphicsFormat.R32FloatX8X24Typeless; return true;
            case Format.FormatX32TypelessG8X24Uint: output = GraphicsFormat.X32TypelessG8X24Uint; return true;
            case Format.FormatR32G8X24Typeless: output = GraphicsFormat.R32G8X24Typeless; return true;
            
            default: output = default; return false;
        }
    }

    public static bool TryConvertToDepthClearFormat(GraphicsFormat format, out Format output) {
        switch (format) {
            case GraphicsFormat.D16UNorm: output = Format.FormatD16Unorm; return true;
            case GraphicsFormat.D24UNormS8UInt: output = Format.FormatD24UnormS8Uint; return true;
            case GraphicsFormat.D32Float: output = Format.FormatD32Float; return true;
            case GraphicsFormat.D32FloatS8X24UInt: output = Format.FormatD32FloatS8X24Uint; return true;
            
            case GraphicsFormat.R16Typeless: output = Format.FormatD16Unorm; return true;
            case GraphicsFormat.R24G8Typeless: output = Format.FormatD24UnormS8Uint; return true;
            case GraphicsFormat.R32Typeless: output = Format.FormatD32Float; return true;
            case GraphicsFormat.R32G8X24Typeless: output = Format.FormatD32FloatS8X24Uint; return true;
            
            default: output = default; return false;
        }
    }
}