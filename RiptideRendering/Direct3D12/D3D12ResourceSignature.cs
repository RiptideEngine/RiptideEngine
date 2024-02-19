using Silk.NET.Direct3D12;
using System.Buffers;

namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12ResourceSignature : ResourceSignature {
    private ComPtr<ID3D12RootSignature> pRoot;
    // private DescriptorTableInfo[] _tableInfos;
    private ResourceParameter[] _parameters;

    public ID3D12RootSignature* RootSignature => pRoot;
    // public ReadOnlySpan<DescriptorTableInfo> TableInfos => _tableInfos;

    public override ReadOnlySpan<ResourceParameter> Parameters => _parameters;

    public D3D12ResourceSignature(D3D12RenderingContext context, ResourceSignatureDescription description) {
        ReadOnlySpan<ResourceParameter> parameterDescs = description.Parameters;
        ReadOnlySpan<ImmutableSamplerDescription> samplerDescs = description.ImmutableSamplers;

        RootParameter[] parameters = ArrayPool<RootParameter>.Shared.Rent(parameterDescs.Length);
        StaticSamplerDesc[] samplers = ArrayPool<StaticSamplerDesc>.Shared.Rent(samplerDescs.Length);

        // _tableInfos = [];

        int numRanges = 0;
        foreach (ref readonly var parameter in parameterDescs) {
            if (parameter.Type != ResourceParameterType.Descriptors) continue;

            numRanges++;
        }

        DescriptorRange[] ranges = ArrayPool<DescriptorRange>.Shared.Rent(numRanges);
        
        try {
            for (int s = 0; s < samplerDescs.Length; s++) {
                ref readonly var input = ref samplerDescs[s];

                samplers[s] = new() {
                    Filter = input.Filter switch {
                        SamplerFilter.Linear => Filter.MinMagMipLinear,
                        SamplerFilter.Anisotropic => Filter.Anisotropic,
                        _ => Filter.MinMagMipPoint,
                    },
                    AddressU = Converting.TryConvert(input.AddressU, out var addr) ? addr : TextureAddressMode.Wrap,
                    AddressV = Converting.TryConvert(input.AddressV, out addr) ? addr : TextureAddressMode.Wrap,
                    AddressW = Converting.TryConvert(input.AddressW, out addr) ? addr : TextureAddressMode.Wrap,

                    BorderColor = StaticBorderColor.TransparentBlack,
                    ComparisonFunc = Converting.TryConvert(input.ComparisonOp, out var comp) ? comp : throw new ArgumentException($"Undefined ImmutableSampler's ComparisonOperator at index {s}."),
                    MinLOD = input.MinLod,
                    MaxLOD = input.MaxLod,
                    MaxAnisotropy = input.MaxAnisotropy,
                    MipLODBias = input.MipLodBias,
                    ShaderVisibility = ShaderVisibility.All,
                    ShaderRegister = input.Register,
                    RegisterSpace = input.Space,
                };
            }

            int rangeOffset = 0;
            for (int p = 0; p < parameterDescs.Length; p++) {
                ref readonly var parameter = ref parameterDescs[p];

                switch (parameter.Type) {
                    case ResourceParameterType.Constants:
                        ref readonly var constants = ref parameter.Constants;

                        parameters[p] = new() {
                            ParameterType = RootParameterType.Type32BitConstants,
                            ShaderVisibility = ShaderVisibility.All,
                            Constants = new() {
                                Num32BitValues = constants.NumConstants,
                                ShaderRegister = constants.Register,
                                RegisterSpace = constants.Space,
                            },
                        };
                        break;

                    case ResourceParameterType.Descriptors:
                        ref readonly var descs = ref parameter.Descriptors;

                        fixed (DescriptorRange* pRanges = ranges) {
                            pRanges[rangeOffset] = new() {
                                OffsetInDescriptorsFromTableStart = 0xFFFFFFFF,
                                RegisterSpace = descs.Space,
                                NumDescriptors = descs.NumDescriptors,
                                BaseShaderRegister = descs.BaseRegister,
                                RangeType = Converting.TryConvert(descs.Type, out var type) ? type : throw new UnreachableException(),
                            };
                            
                            parameters[p] = new() {
                                ParameterType = RootParameterType.TypeDescriptorTable,
                                ShaderVisibility = ShaderVisibility.All,
                                DescriptorTable = new() {
                                    NumDescriptorRanges = 1,
                                    PDescriptorRanges = pRanges + rangeOffset,
                                },
                            };
                        }

                        rangeOffset++;
                        break;
                }
            }

            fixed (RootParameter* pParameters = parameters) {
                fixed (StaticSamplerDesc* pSamplers = samplers) {
                    RootSignatureDesc rdesc = new() {
                        Flags = RootSignatureFlags.None,
                        NumParameters = (uint)parameterDescs.Length,
                        PParameters = pParameters,
                        NumStaticSamplers = (uint)samplerDescs.Length,
                        PStaticSamplers = pSamplers,
                    };
                    
                    using ComPtr<ID3D10Blob> pSerialized = default;
                    using ComPtr<ID3D10Blob> pError = default;
                    HResult hr = context.D3D12.SerializeRootSignature(&rdesc, D3DRootSignatureVersion.Version10, pSerialized.GetAddressOf(), pError.GetAddressOf());
                    if (hr < 0) {
                        throw new($"Failed to serialize Root Signature. Error: '{new string((sbyte*)pError.GetBufferPointer(), 0, (int)pError.GetBufferSize())}'.");
                    }

                    hr = context.Device->CreateRootSignature(1, pSerialized.GetBufferPointer(), pSerialized.GetBufferSize(), SilkMarshal.GuidPtrOf<ID3D12RootSignature>(), (void**)pRoot.GetAddressOf());
                    Marshal.ThrowExceptionForHR(hr);
                }
            }

        } finally {
            ArrayPool<RootParameter>.Shared.Return(parameters);
            ArrayPool<DescriptorRange>.Shared.Return(ranges);
            ArrayPool<StaticSamplerDesc>.Shared.Return(samplers);
        }

        _parameters = new ResourceParameter[description.Parameters.Length];
        Array.Copy(description.Parameters, _parameters, _parameters.Length);
        
        // _tableInfos = new DescriptorTableInfo[numRanges];
        // int tableIndex = 0;
        //
        // for (int i = 0; i < description.Parameters.Length; i++) {
        //     ref readonly var parameter = ref description.Parameters[i];
        //     if (parameter.Type != ResourceParameterType.Descriptors) continue;
        //
        //     uint numDescriptors = parameter.Descriptors.NumDescriptors & ~(0b11 << 30);
        //     uint type = (uint)((int)parameter.Type << 30);
        //
        //     _tableInfos[tableIndex++] = new((uint)i, numDescriptors | type);
        // }

        _refcount = 1;
    }

    protected override void Dispose() {
        pRoot.Release();
        pRoot = default;

        _parameters = [];
    }

    /// <summary>
    /// Contains information of descriptor tables that <see cref="GraphicsDescriptorCommitter"/> can consume.
    /// </summary>
    /// <param name="ParameterIndex">Parameter index.</param>
    /// <param name="Bitfield">Bitfield of the table. Bits 0-29 represent amount of descriptors. Bit 30 and 31 determine resource type based on <see cref="DescriptorTableType"/>.</param>
    public readonly record struct DescriptorTableInfo(uint ParameterIndex, uint Bitfield) {
        public readonly bool IsSampler => (DescriptorTableType)(Bitfield >> 30) == DescriptorTableType.Sampler;
        public readonly uint NumDescriptors => Bitfield & ~(0b11 << 30);
    }
}