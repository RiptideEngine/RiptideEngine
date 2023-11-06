using RiptideRendering.Shadering;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12PipelineState : PipelineState {
    private const string UnnamedPipelineState = $"<Unnamed {nameof(D3D12PipelineState)}>.pPipelineState";

    private ComPtr<ID3D12PipelineState> pPipelineState;
    public ID3D12PipelineState* PipelineState => pPipelineState;

    public override string? Name {
        get => base.Name;
        set {
            base.Name = value;

            if (pPipelineState.Handle != null) {
                if (value == null) {
                    D3D12Helper.SetName((ID3D12Object*)pPipelineState.Handle, UnnamedPipelineState);
                } else {
                    var reqLength = value.Length + 1 + nameof(pPipelineState).Length;
                    var charArray = ArrayPool<char>.Shared.Rent(reqLength);
                    {
                        value.CopyTo(charArray);
                        charArray[value.Length] = '.';
                        nameof(pPipelineState).CopyTo(0, charArray, value.Length + 1, nameof(pPipelineState).Length);

                        D3D12Helper.SetName((ID3D12Object*)pPipelineState.Handle, charArray.AsSpan(0, reqLength));
                    }
                    ArrayPool<char>.Shared.Return(charArray);
                }
            }
        }
    }

    public D3D12PipelineState(D3D12RenderingContext context, D3D12GraphicalShader shader, D3D12ResourceSignature pipelineResource, in PipelineStateConfig config) {
        ShaderBytecode vs, ps, hs, ds;
        RasterizerDesc rdesc;
        DepthStencilDesc dsdesc;
        BlendDesc bdesc;
        Unsafe.SkipInit(out GraphicsPipelineStateDesc.RTVFormatsBuffer rtFormats);

        // Converting data
        {
            vs = D3D12Helper.CreateShaderBytecode((IDxcBlob*)shader.VertexShaderHandle);
            ps = D3D12Helper.CreateShaderBytecode((IDxcBlob*)shader.PixelShaderHandle);

            if (shader.HasTessellationStages) {
                hs = D3D12Helper.CreateShaderBytecode((IDxcBlob*)shader.HullShaderHandle);
                ds = D3D12Helper.CreateShaderBytecode((IDxcBlob*)shader.DomainShaderHandle);
            } else {
                hs = ds = default;
            }

            ref readonly var rtConfig = ref config.RenderTargetFormats;

            D3D12Convert.TryConvert(config.RenderTargetFormats.Formats[0], out rtFormats[0]);
            D3D12Convert.TryConvert(config.RenderTargetFormats.Formats[1], out rtFormats[1]);
            D3D12Convert.TryConvert(config.RenderTargetFormats.Formats[2], out rtFormats[2]);
            D3D12Convert.TryConvert(config.RenderTargetFormats.Formats[3], out rtFormats[3]);
            D3D12Convert.TryConvert(config.RenderTargetFormats.Formats[4], out rtFormats[4]);
            D3D12Convert.TryConvert(config.RenderTargetFormats.Formats[5], out rtFormats[5]);
            D3D12Convert.TryConvert(config.RenderTargetFormats.Formats[6], out rtFormats[6]);
            D3D12Convert.TryConvert(config.RenderTargetFormats.Formats[7], out rtFormats[7]);

            ref readonly var rasterConfig = ref config.Rasterization;
            rdesc = new() {
                CullMode = rasterConfig.CullMode switch {
                    CullingMode.None => CullMode.None,
                    CullingMode.Front => CullMode.Front,
                    CullingMode.Back or _ => CullMode.Back,
                },
                FillMode = rasterConfig.FillMode switch {
                    FillingMode.Wireframe => FillMode.Wireframe,
                    FillingMode.Solid or _ => FillMode.Solid,
                },
                ConservativeRaster = rasterConfig.Conservative ? ConservativeRasterizationMode.On : ConservativeRasterizationMode.Off,
                DepthClipEnable = true,
            };

            ref readonly var blendConfig = ref config.Blending;
            bdesc = new() {
                AlphaToCoverageEnable = false,
                IndependentBlendEnable = blendConfig.Independent,
            };

            bdesc.RenderTarget[0] = ConvertTargetBlend(config.Blending.Blends[0]);
            bdesc.RenderTarget[1] = ConvertTargetBlend(config.Blending.Blends[1]);
            bdesc.RenderTarget[2] = ConvertTargetBlend(config.Blending.Blends[2]);
            bdesc.RenderTarget[3] = ConvertTargetBlend(config.Blending.Blends[3]);
            bdesc.RenderTarget[4] = ConvertTargetBlend(config.Blending.Blends[4]);
            bdesc.RenderTarget[5] = ConvertTargetBlend(config.Blending.Blends[5]);
            bdesc.RenderTarget[6] = ConvertTargetBlend(config.Blending.Blends[6]);
            bdesc.RenderTarget[7] = ConvertTargetBlend(config.Blending.Blends[7]);

            ref readonly var dsconfig = ref config.DepthStencil;
            dsdesc = new() {
                DepthEnable = dsconfig.EnableDepth,
                StencilEnable = dsconfig.EnableStencil,
                DepthWriteMask = DepthWriteMask.All,
                StencilReadMask = D3D12.DefaultStencilReadMask,
                StencilWriteMask = D3D12.DefaultStencilWriteMask,
                DepthFunc = D3D12Convert.TryConvert(dsconfig.DepthComparison, out var depthFunc) ? depthFunc : ComparisonFunc.Never,
                BackFace = Unsafe.BitCast<StencilOperationConfig, DepthStencilopDesc>(dsconfig.BackfaceOperation),
                FrontFace = Unsafe.BitCast<StencilOperationConfig, DepthStencilopDesc>(dsconfig.FrontFaceOperation),
            };
        }

        bool cvt = D3D12Convert.TryConvert(config.DepthFormat, out var depthFormat);
        Debug.Assert(cvt);

        GraphicsPipelineStateDesc desc = new() {
            VS = vs, PS = ps, HS = hs, DS = ds,
            PRootSignature = pipelineResource.RootSignature,
            SampleMask = uint.MaxValue,
            SampleDesc = new() {
                Count = 1,
                Quality = 0,
            },
            RasterizerState = rdesc,
            DepthStencilState = dsdesc,
            BlendState = bdesc,
            RTVFormats = rtFormats,
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
            DSVFormat = depthFormat,
            NumRenderTargets = config.RenderTargetFormats.NumRenderTargets,
            NodeMask = 1,
        };

        using ComPtr<ID3D12PipelineState> pOutput = default;
        int hr = context.Device->CreateGraphicsPipelineState(&desc, SilkMarshal.GuidPtrOf<ID3D12PipelineState>(), (void**)pOutput.GetAddressOf());
        Marshal.ThrowExceptionForHR(hr);

        D3D12Helper.SetName(pOutput.Handle, UnnamedPipelineState);
        pPipelineState.Handle = pOutput.Detach();

        Shader = shader;
        Shader.IncrementReference();

        Type = PipelineStateType.Graphical;
        _refcount = 1;

        static RenderTargetBlendDesc ConvertTargetBlend(RenderTargetBlendConfig config) {
            return new() {
                BlendEnable = config.EnableBlend,
                RenderTargetWriteMask = (byte)config.WriteMask,

                LogicOpEnable = false,
                LogicOp = LogicOp.Noop,

                SrcBlend = (Blend)config.Source,
                DestBlend = (Blend)config.Dest,
                BlendOp = (BlendOp)config.Operator,

                SrcBlendAlpha = (Blend)config.SourceAlpha,
                DestBlendAlpha = (Blend)config.DestAlpha,
                BlendOpAlpha = (BlendOp)config.AlphaOperator,
            };
        }
    }

    protected override void Dispose() {
        Shader.DecrementReference(); Shader = null!;
        pPipelineState.Dispose(); pPipelineState = default;
    }
}