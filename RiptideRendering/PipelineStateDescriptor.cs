namespace RiptideRendering;

public enum FillingMode {
    Unknown,
    Wireframe,
    Solid,
}
public enum CullingMode {
    None,
    Front,
    Back,
}

public struct RasterizationConfig {
    public static RasterizationConfig Default => new() {
        CullMode = CullingMode.Back,
        FillMode = FillingMode.Solid,
        Conservative = false,
    };

    public FillingMode FillMode;
    public CullingMode CullMode;
    public bool Conservative;
}

public enum StencilOperation {
    Keep = 1,
    Zero = 2,
    Replace = 3,
    IncreaseSaturated = 4,
    DecreateSaturated = 5,
    Invert = 6,
    Increase = 7,
    Decrease = 8
}

public struct StencilOperationConfig {
    public StencilOperation DepthFailOp;
    public StencilOperation FailOp;
    public StencilOperation PassOp;
    public ComparisonOperator CompareOp;
}

public struct RenderTargetFormatConfig {
    public uint NumRenderTargets;
    public RenderTargetFormats Formats;
}

[InlineArray(8)]
public struct RenderTargetFormats {
    private GraphicsFormat _element0;
}

public struct DepthStencilConfig {
    public bool EnableDepth;
    public bool EnableStencil;
    public ComparisonOperator DepthComparison;
    public StencilOperationConfig BackfaceOperation;
    public StencilOperationConfig FrontFaceOperation;
}

public enum RenderTargetWriteMask : byte {
    None = 0,

    Red = 1 << 0,
    Green = 1 << 1,
    Blue = 1 << 2,
    Alpha = 1 << 3,

    All = Red | Green | Blue | Alpha,
}

public enum BlendFactor {
    Zero = 1,
    One = 2,
    SourceColor = 3,
    InvertSourceColor = 4,
    SourceAlpha = 5,
    InvertSourceAlpha = 6,
    DestinationAlpha = 7,
    InvertDestinationAlpha = 8,
    DestinationColor = 9,
    InvertDestinationColor = 10,
    SsourceAlphaSaturated = 11,
    BlendFactor = 14,
    InvertBlendFactor = 15,
    Source1Color = 16,
    InvertSource1Color = 17,
    Source1Alpha = 18,
    InvertSource1Alpha = 19,
    AlphaFactor = 20,
    InvertAlphaFactor = 21
}

public enum BlendOperator {
    Add = 1,
    Subtract = 2,
    RevSubtract = 3,
    Min = 4,
    Max = 5
}

public struct RenderTargetBlendConfig {
    public bool EnableBlend;
    public RenderTargetWriteMask WriteMask;

    public BlendFactor Source;
    public BlendFactor Dest;
    public BlendOperator Operator;

    public BlendFactor SourceAlpha;
    public BlendFactor DestAlpha;
    public BlendOperator AlphaOperator;
}

[InlineArray(8)]
public struct RenderTargetBlends {
    private RenderTargetBlendConfig _element0;
}

public struct BlendingConfig {
    public static BlendingConfig Opaque {
        get {
            BlendingConfig output = new() {
                Independent = false,
            };

            output.Blends[0] = new() {
                EnableBlend = true,
                WriteMask = RenderTargetWriteMask.All,
                Source = BlendFactor.One,
                Dest = BlendFactor.Zero,
                Operator = BlendOperator.Add,
                SourceAlpha = BlendFactor.One,
                DestAlpha = BlendFactor.Zero,
                AlphaOperator = BlendOperator.Add,
            };

            return output;
        }
    }
    public static BlendingConfig Disable {
        get {
            BlendingConfig output = new() {
                Independent = false,
            };

            output.Blends[0] = new() {
                EnableBlend = false,
                WriteMask = RenderTargetWriteMask.All,
                Source = BlendFactor.One,
                Dest = BlendFactor.Zero,
                Operator = BlendOperator.Add,
                SourceAlpha = BlendFactor.One,
                DestAlpha = BlendFactor.Zero,
                AlphaOperator = BlendOperator.Add,
            };

            return output;
        }
    }
    public static BlendingConfig Transparent {
        get {
            BlendingConfig output = new() {
                Independent = false,
            };

            output.Blends[0] = new() {
                EnableBlend = true,
                WriteMask = RenderTargetWriteMask.All,
                Source = BlendFactor.SourceAlpha,
                Dest = BlendFactor.InvertSourceAlpha,
                Operator = BlendOperator.Add,
                SourceAlpha = BlendFactor.One,
                DestAlpha = BlendFactor.InvertSourceAlpha,
                AlphaOperator = BlendOperator.Add,
            };

            return output;
        }
    }

    public bool Independent;
    public RenderTargetBlends Blends;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BlendingConfig CreateNonIndependent(RenderTargetBlendConfig config) {
        BlendingConfig output = new() {
            Independent = false,
        };
        output.Blends[0] = config;

        return output;
    }
}

public struct PipelineStateDescriptor {
    public required RasterizationConfig Rasterization;
    public required DepthStencilConfig DepthStencil;
    public required BlendingConfig Blending;
    public required RenderTargetFormatConfig RenderTargetFormats;
    public required GraphicsFormat DepthFormat;
    public required PipelinePrimitiveTopology PrimitiveTopology;
}