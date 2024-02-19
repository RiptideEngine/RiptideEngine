using Silk.NET.Direct3D12;
using System.Buffers;

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
                    Helper.SetName((ID3D12Object*)pPipelineState.Handle, UnnamedPipelineState);
                } else {
                    var reqLength = value.Length + 1 + nameof(pPipelineState).Length;
                    var charArray = ArrayPool<char>.Shared.Rent(reqLength);
                    {
                        value.CopyTo(charArray);
                        charArray[value.Length] = '.';
                        nameof(pPipelineState).CopyTo(0, charArray, value.Length + 1, nameof(pPipelineState).Length);

                        Helper.SetName((ID3D12Object*)pPipelineState.Handle, charArray.AsSpan(0, reqLength));
                    }
                    ArrayPool<char>.Shared.Return(charArray);
                }
            }
        }
    }

    public D3D12PipelineState(D3D12RenderingContext context, D3D12GraphicalShader shader, D3D12ResourceSignature signature, in PipelineStateDescription description) {
        Unsafe.SkipInit(out GraphicsPipelineStateDesc.RTVFormatsBuffer rtFormats);

        fixed (byte* pVSBytecode = shader.VSBytecode) {
            fixed (byte* pPSBytecode = shader.PSBytecode) {
                fixed (byte* pHSBytecode = shader.HSBytecode) {
                    fixed (byte* pDSBytecode = shader.DSBytecode) {
                        ref readonly var rtConfig = ref description.RenderTargetFormats;

                        bool convert = Converting.TryConvert(rtConfig.Formats[0], out rtFormats[0]);
                        if (!convert) throw new ArgumentException("Failed to convert RenderTargetFormats index 0.");
                        
                        convert = Converting.TryConvert(rtConfig.Formats[1], out rtFormats[1]);
                        if (!convert) throw new ArgumentException("Failed to convert RenderTargetFormats index 1.");
                        
                        convert = Converting.TryConvert(rtConfig.Formats[2], out rtFormats[2]);
                        if (!convert) throw new ArgumentException("Failed to convert RenderTargetFormats index 2.");
                        
                        convert = Converting.TryConvert(rtConfig.Formats[3], out rtFormats[3]);
                        if (!convert) throw new ArgumentException("Failed to convert RenderTargetFormats index 3.");
                        
                        convert = Converting.TryConvert(rtConfig.Formats[4], out rtFormats[4]);
                        if (!convert) throw new ArgumentException("Failed to convert RenderTargetFormats index 4.");
                        
                        convert = Converting.TryConvert(rtConfig.Formats[5], out rtFormats[5]);
                        if (!convert) throw new ArgumentException("Failed to convert RenderTargetFormats index 5.");
                        
                        convert = Converting.TryConvert(rtConfig.Formats[6], out rtFormats[6]);
                        if (!convert) throw new ArgumentException("Failed to convert RenderTargetFormats index 6.");
                        
                        convert = Converting.TryConvert(rtConfig.Formats[7], out rtFormats[7]);
                        if (!convert) throw new ArgumentException("Failed to convert RenderTargetFormats index 7.");

                        ref readonly var rasterConfig = ref description.Rasterization;
                        RasterizerDesc rdesc = new() {
                            CullMode = rasterConfig.CullMode switch {
                                CullingMode.None => CullMode.None,
                                CullingMode.Front => CullMode.Front,
                                _ => CullMode.Back,
                            },
                            FillMode = rasterConfig.FillMode switch {
                                FillingMode.Wireframe => FillMode.Wireframe,
                                _ => FillMode.Solid,
                            },
                            ConservativeRaster = rasterConfig.Conservative ? ConservativeRasterizationMode.On : ConservativeRasterizationMode.Off,
                            DepthClipEnable = true,
                        };

                        ref readonly var blendConfig = ref description.Blending;
                        BlendDesc bdesc = new() {
                            AlphaToCoverageEnable = false,
                            IndependentBlendEnable = blendConfig.Independent,
                        };

                        bdesc.RenderTarget[0] = ConvertTargetBlend(description.Blending.Blends[0]);
                        bdesc.RenderTarget[1] = ConvertTargetBlend(description.Blending.Blends[1]);
                        bdesc.RenderTarget[2] = ConvertTargetBlend(description.Blending.Blends[2]);
                        bdesc.RenderTarget[3] = ConvertTargetBlend(description.Blending.Blends[3]);
                        bdesc.RenderTarget[4] = ConvertTargetBlend(description.Blending.Blends[4]);
                        bdesc.RenderTarget[5] = ConvertTargetBlend(description.Blending.Blends[5]);
                        bdesc.RenderTarget[6] = ConvertTargetBlend(description.Blending.Blends[6]);
                        bdesc.RenderTarget[7] = ConvertTargetBlend(description.Blending.Blends[7]);

                        ref readonly var dsconfig = ref description.DepthStencil;
                        DepthStencilDesc dsdesc = new() {
                            DepthEnable = dsconfig.EnableDepth,
                            StencilEnable = dsconfig.EnableStencil,
                            DepthWriteMask = dsconfig.DepthWrite ? DepthWriteMask.All : DepthWriteMask.Zero,
                            StencilReadMask = dsconfig.StencilReadMask,
                            StencilWriteMask = dsconfig.StencilWriteMask,
                            DepthFunc = Converting.TryConvert(dsconfig.DepthComparison, out var depthFunc) ? depthFunc : ComparisonFunc.Never,
                            BackFace = new() {
                                StencilDepthFailOp = (StencilOp)dsconfig.BackfaceOperation.DepthFailOp,
                                StencilFailOp = (StencilOp)dsconfig.BackfaceOperation.FailOp,
                                StencilPassOp = (StencilOp)dsconfig.BackfaceOperation.PassOp,
                                StencilFunc = (ComparisonFunc)(dsconfig.BackfaceOperation.CompareOp + 1),
                            },
                            FrontFace = new() {
                                StencilDepthFailOp = (StencilOp)dsconfig.FrontFaceOperation.DepthFailOp,
                                StencilFailOp = (StencilOp)dsconfig.FrontFaceOperation.FailOp,
                                StencilPassOp = (StencilOp)dsconfig.FrontFaceOperation.PassOp,
                                StencilFunc = (ComparisonFunc)(dsconfig.FrontFaceOperation.CompareOp + 1),
                            },
                        };

                        convert = Converting.TryConvert(description.DepthFormat, out var depthFormat);
                        if (!convert) throw new ArgumentException("Failed to convert depth format.");

                        GraphicsPipelineStateDesc desc = new() {
                            VS = new() {
                                PShaderBytecode = pVSBytecode,
                                BytecodeLength = (nuint)shader.VSBytecode.Length,
                            },
                            HS = new() {
                                PShaderBytecode = pHSBytecode,
                                BytecodeLength = (nuint)shader.HSBytecode.Length,
                            },
                            DS = new() {
                                PShaderBytecode = pDSBytecode,
                                BytecodeLength = (nuint)shader.DSBytecode.Length,
                            },
                            PS = new() {
                                PShaderBytecode = pPSBytecode,
                                BytecodeLength = (nuint)shader.PSBytecode.Length,
                            },

                            PRootSignature = signature.RootSignature,
                            SampleMask = uint.MaxValue,
                            SampleDesc = new() {
                                Count = 1,
                                Quality = 0,
                            },
                            RasterizerState = rdesc,
                            DepthStencilState = dsdesc,
                            BlendState = bdesc,
                            RTVFormats = rtFormats,
                            PrimitiveTopologyType = (PrimitiveTopologyType)description.PrimitiveTopology,
                            DSVFormat = depthFormat,
                            NumRenderTargets = description.RenderTargetFormats.NumRenderTargets,
                            NodeMask = 1,
                        };

                        int hr = context.Device->CreateGraphicsPipelineState(&desc, SilkMarshal.GuidPtrOf<ID3D12PipelineState>(), (void**)pPipelineState.GetAddressOf());
                        Marshal.ThrowExceptionForHR(hr);

                        Helper.SetName(pPipelineState.Handle, UnnamedPipelineState);
                    }
                }
            }
        }
        
        _refcount = 1;

        static RenderTargetBlendDesc ConvertTargetBlend(RenderTargetBlendDescription config) {
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

    public D3D12PipelineState(D3D12RenderingContext context, D3D12ComputeShader shader, D3D12ResourceSignature signature) {
        fixed (byte* pBytecode = shader.Bytecode) {
            ComputePipelineStateDesc desc = new() {
                CS = new() { PShaderBytecode = pBytecode, BytecodeLength = (nuint)shader.Bytecode.Length },
                PRootSignature = signature.RootSignature,
            };

            int hr = context.Device->CreateComputePipelineState(&desc, SilkMarshal.GuidPtrOf<ID3D12PipelineState>(), (void**)pPipelineState.GetAddressOf());
            Marshal.ThrowExceptionForHR(hr);

            Helper.SetName(pPipelineState.Handle, UnnamedPipelineState);

            _refcount = 1;
        }
    }

    protected override void Dispose() {
        pPipelineState.Dispose(); pPipelineState = default;
    }
}