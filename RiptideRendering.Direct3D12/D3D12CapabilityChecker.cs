namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12CapabilityChecker : BaseCapabilityChecker {
    private readonly D3D12RenderingContext _context;
    private readonly ShaderModel _highestSM;

    public override ShaderModel HighestShaderModel => _highestSM;
    public override bool UseRootSignature => true;
    public ResourceBindingTier ResourceBindingTier { get; private set; }
    public override bool SupportShaderSpecifiedStencilRef { get; }

    public bool SupportBindless => ResourceBindingTier >= ResourceBindingTier.Tier3 && _highestSM >= ShaderModel.SM6_6;
    public override bool SupportRootConstants => true;

    public D3D12CapabilityChecker(D3D12RenderingContext context) {
        if (!D3D12Convert.TryConvert(context.Constants.HighestShaderModelVersion, out _highestSM)) {
            throw new Exception("Failed to convert highest shader model version.");
        }

        _context = context;

        {
            FeatureDataD3D12Options data;
            int hr = context.Device->CheckFeatureSupport(D3D12Feature.D3D12Options, &data, (uint)sizeof(FeatureDataD3D12Options));
            Debug.Assert(hr >= 0);

            ResourceBindingTier = data.ResourceBindingTier;
            SupportShaderSpecifiedStencilRef = data.PSSpecifiedStencilRefSupported;
        }
    }

    public override TextureSupportFlags CheckTextureFormatSupport(GraphicsFormat format) {
        if (!D3D12Convert.TryConvert(format, out var dxgiFormat)) return TextureSupportFlags.None;

        FeatureDataFormatSupport data = new() {
            Format = dxgiFormat,
        };
        int hr = _context.Device->CheckFeatureSupport(D3D12Feature.FormatSupport, &data, (uint)sizeof(FeatureDataFormatSupport));
        Debug.Assert(hr >= 0);

        int converting = ((int)(data.Support1 & (FormatSupport1.Texture1D | FormatSupport1.Texture2D | FormatSupport1.Texture3D | FormatSupport1.Texturecube)) >> 4) |
            ((int)(data.Support1 & FormatSupport1.RenderTarget) >> 10) |
            ((int)(data.Support1 & FormatSupport1.DepthStencil) >> 11);

        return (TextureSupportFlags)converting;
    }

    public bool CheckFormatSupport(GraphicsFormat format, FormatSupport1 support) {
        if (!D3D12Convert.TryConvert(format, out var dxgiFormat)) return false;

        return CheckFormatSupport(dxgiFormat, support);
    }

    public bool CheckFormatSupport(Format format, FormatSupport1 support) {
        FeatureDataFormatSupport data = new() {
            Format = format,
        };
        int hr = _context.Device->CheckFeatureSupport(D3D12Feature.FormatSupport, &data, (uint)sizeof(FeatureDataFormatSupport));
        Debug.Assert(hr >= 0);

        return data.Support1.HasFlag(support);
    }
}