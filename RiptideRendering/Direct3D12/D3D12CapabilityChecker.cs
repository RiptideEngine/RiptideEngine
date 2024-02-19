using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12CapabilityChecker : CapabilityChecker {
    private readonly D3D12RenderingContext _context;

    public override bool SupportShaderSpecifiedStencilRef { get; }
    public override bool SupportMeshShader { get; }

    public D3D12CapabilityChecker(D3D12RenderingContext context) {
        _context = context;
        
        FeatureDataD3D12Options options;
        context.Device->CheckFeatureSupport(Feature.D3D12Options, &options, (uint)sizeof(FeatureDataD3D12Options));

        SupportShaderSpecifiedStencilRef = options.PSSpecifiedStencilRefSupported;

        FeatureDataD3D12Options7 options7;
        context.Device->CheckFeatureSupport(Feature.D3D12Options7, &options7, (uint)sizeof(FeatureDataD3D12Options7));

        SupportMeshShader = options7.MeshShaderTier >= MeshShaderTier.Tier1;
    }

    public override TextureSupportFlags CheckTextureFormatSupport(GraphicsFormat format) {
        if (!Converting.TryConvert(format, out var dxgiFormat)) return TextureSupportFlags.None;
        
        FeatureDataFormatSupport data = new() {
            Format = dxgiFormat,
        };
        _context.Device->CheckFeatureSupport(Feature.FormatSupport, &data, (uint)sizeof(FeatureDataFormatSupport));

        return (TextureSupportFlags)((int)(data.Support1 & (FormatSupport1.Texture1D | FormatSupport1.Texture2D | FormatSupport1.Texture3D | FormatSupport1.Texturecube)) >> 4 | (int)(data.Support1 & FormatSupport1.RenderTarget) >> 10 | (int)(data.Support1 & FormatSupport1.DepthStencil) >> 11);
    }

    public override (uint Dimension, uint Array) GetMaximumTextureSize(TextureDimension dimension) {
        return dimension switch {
            TextureDimension.Texture1D => (16384, 2048),
            TextureDimension.Texture2D => (16384, 2048),
            TextureDimension.Texture3D => (2048, 2048),
            _ => (0, 0),
        };
    }

    public override bool CheckTextureMipmapSupport(GraphicsFormat format) {
        if (!Converting.TryConvert(format, out var dxgiFormat)) return false;
        
        FeatureDataFormatSupport data = new() {
            Format = dxgiFormat,
        };
        _context.Device->CheckFeatureSupport(Feature.FormatSupport, &data, (uint)sizeof(FeatureDataFormatSupport));

        return data.Support1.HasFlag(FormatSupport1.Mip);
    }
}