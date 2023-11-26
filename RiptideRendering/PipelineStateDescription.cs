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

public struct RasterizerDescription {
    public static RasterizerDescription Default => new() {
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

public struct StencilOperationDescription {
    public StencilOperation DepthFailOp;
    public StencilOperation FailOp;
    public StencilOperation PassOp;
    public ComparisonOperator CompareOp;
}

public struct RenderTargetFormatDescription {
    public uint NumRenderTargets;
    public RenderTargetFormats Formats;
}

[InlineArray(8)]
public struct RenderTargetFormats {
    private GraphicsFormat _element0;
}

public struct DepthStencilDescription {
    public static DepthStencilDescription Default => new() {
        EnableDepth = false,
        DepthWrite = true,
        EnableStencil = false,
        StencilReadMask = 0xFF,
        StencilWriteMask = 0xFF,
        DepthComparison = ComparisonOperator.Less,
        BackfaceOperation = new() {
            DepthFailOp = StencilOperation.Keep,
            FailOp = StencilOperation.Keep,
            PassOp = StencilOperation.Keep,
            CompareOp = ComparisonOperator.Always,
        },
        FrontFaceOperation = new() {
            DepthFailOp = StencilOperation.Keep,
            FailOp = StencilOperation.Keep,
            PassOp = StencilOperation.Keep,
            CompareOp = ComparisonOperator.Always,
        },
    };

    public static DepthStencilDescription Disable => new() {
        EnableDepth = false,
        EnableStencil = false,
        DepthComparison = ComparisonOperator.Always,
        BackfaceOperation = new() {
            DepthFailOp = StencilOperation.Keep,
            PassOp = StencilOperation.Keep,
            FailOp = StencilOperation.Keep,
            CompareOp = ComparisonOperator.Always,
        },
        FrontFaceOperation = new() {
            DepthFailOp = StencilOperation.Keep,
            PassOp = StencilOperation.Keep,
            FailOp = StencilOperation.Keep,
            CompareOp = ComparisonOperator.Always,
        },
        DepthWrite = true,
    };
    
    public bool EnableDepth;
    public bool DepthWrite;
    public ComparisonOperator DepthComparison;
    public bool EnableStencil;
    public byte StencilReadMask;
    public byte StencilWriteMask;
    public StencilOperationDescription BackfaceOperation;
    public StencilOperationDescription FrontFaceOperation;
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

public struct RenderTargetBlendDescription {
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
    private RenderTargetBlendDescription _element0;
}

public struct BlendingDescription {
    public static BlendingDescription Opaque {
        get {
            BlendingDescription output = new() {
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
    public static BlendingDescription Disable {
        get {
            BlendingDescription output = new() {
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
    public static BlendingDescription Transparent {
        get {
            BlendingDescription output = new() {
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
    public static BlendingDescription CreateNonIndependent(RenderTargetBlendDescription description) {
        BlendingDescription output = new() {
            Independent = false,
        };
        output.Blends[0] = description;

        return output;
    }
}

public struct PipelineStateDescription {
    public required RasterizerDescription Rasterization;
    public required DepthStencilDescription DepthStencil;
    public required BlendingDescription Blending;
    public required RenderTargetFormatDescription RenderTargetFormats;
    public required GraphicsFormat DepthFormat;
    public required PipelinePrimitiveTopology PrimitiveTopology;
}