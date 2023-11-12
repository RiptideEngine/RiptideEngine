namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12ResourceSignature : ResourceSignature {
    private ComPtr<ID3D12RootSignature> pRoot;
    private DescriptorTableInfo[] _tableInfos;

    public ID3D12RootSignature* RootSignature => pRoot;
    public ReadOnlySpan<DescriptorTableInfo> TableInfos => _tableInfos;

    public D3D12ResourceSignature(D3D12RenderingContext context, ResourceSignatureDescriptor descriptor) {
        ReadOnlySpan<ResourceParameter> parameterDescs = descriptor.Parameters;
        ReadOnlySpan<ImmutableSamplerDescriptor> samplerDescs = descriptor.ImmutableSamplers;

        RootParameter[] parameters = ArrayPool<RootParameter>.Shared.Rent(parameterDescs.Length);
        StaticSamplerDesc[] samplers = ArrayPool<StaticSamplerDesc>.Shared.Rent(samplerDescs.Length);

        _tableInfos = [];

        uint numTables = 0;
        int numRanges = 0;
        foreach (ref readonly var parameter in parameterDescs) {
            if (parameter.Type != ResourceParameterType.Table) continue;

            ref readonly var table = ref parameter.Table;

            numTables++;
            numRanges += table.Ranges.Length;
        }

        DescriptorRange[] ranges = ArrayPool<DescriptorRange>.Shared.Rent(numRanges);
        
        try {
            for (int s = 0; s < samplerDescs.Length; s++) {
                ref readonly var input = ref samplerDescs[s];

                samplers[s] = new() {
                    Filter = input.Filter switch {
                        SamplerFilter.Linear => Filter.MinMagMipLinear,
                        SamplerFilter.Anisotropic => Filter.Anisotropic,
                        SamplerFilter.Point or _ => Filter.MinMagMipPoint,
                    },
                    AddressU = D3D12Convert.TryConvert(input.AddressU, out var addr) ? addr : TextureAddressMode.Wrap,
                    AddressV = D3D12Convert.TryConvert(input.AddressU, out addr) ? addr : TextureAddressMode.Wrap,
                    AddressW = D3D12Convert.TryConvert(input.AddressU, out addr) ? addr : TextureAddressMode.Wrap,

                    BorderColor = StaticBorderColor.TransparentBlack,
                    ComparisonFunc = D3D12Convert.TryConvert(input.ComparisonOp, out var comp) ? comp : throw new ArgumentException($"Undefined ImmutableSampler's ComparisonOperator at index {s}."),
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

                    case ResourceParameterType.Table:
                        ref readonly var table = ref parameter.Table;

                        for (int r = 0; r < table.Ranges.Length; r++) {
                            ref readonly var range = ref table.Ranges[r];

                            ranges[rangeOffset + r] = new() {
                                OffsetInDescriptorsFromTableStart = 0xFFFFFFFF,
                                NumDescriptors = range.NumResources,
                                RegisterSpace = range.Space,
                                BaseShaderRegister = range.BaseRegister,
                                RangeType = D3D12Convert.TryConvert(range.Type, out var cvt) ? cvt : throw new ArgumentException($"Failed to convert ResourceRangeType value {range.Type} to correspond Direct3D12's value."),
                            };
                        }

                        fixed (DescriptorRange* pRanges = ranges) {
                            parameters[p] = new() {
                                ParameterType = RootParameterType.TypeDescriptorTable,
                                ShaderVisibility = ShaderVisibility.All,
                                DescriptorTable = new() {
                                    NumDescriptorRanges = (uint)table.Ranges.Length,
                                    PDescriptorRanges = pRanges + rangeOffset,
                                },
                            };
                        }

                        rangeOffset += table.Ranges.Length;
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

                    context.RootSigStorage.Get(rdesc, pRoot.GetAddressOf());
                }
            }

        } finally {
            ArrayPool<RootParameter>.Shared.Return(parameters);
            ArrayPool<DescriptorRange>.Shared.Return(ranges);
            ArrayPool<StaticSamplerDesc>.Shared.Return(samplers);
        }

        _tableInfos = new DescriptorTableInfo[numTables];
        int tableIndex = 0;

        for (int i = 0; i < descriptor.Parameters.Length; i++) {
            ref readonly var parameter = ref descriptor.Parameters[i];
            if (parameter.Type != ResourceParameterType.Table) continue;

            ref readonly var table = ref parameter.Table;

            uint numDescriptor = 0;

            foreach (ref readonly var range in table.Ranges.AsSpan()) {
                numDescriptor += range.NumResources;
            }

            _tableInfos[tableIndex++] = new((uint)i, numDescriptor | (table.Ranges[0].Type == ResourceRangeType.Sampler ? 1U << 31 : 0));
        }

        _refcount = 1;
    }

    protected override void Dispose() {
        pRoot = default;
        _tableInfos = [];
    }

    /// <summary>
    /// Contains information of descriptor tables that <see cref="DescriptorCommitter"/> can consume.
    /// </summary>
    /// <param name="ParameterIndex">Parameter index.</param>
    /// <param name="Bitmap">Bitfield of the table. Bits 0-31 represent amount of descriptors. Bit 32 determine whether table is sampler or resource, 1 if sampler.</param>
    public readonly record struct DescriptorTableInfo(uint ParameterIndex, uint Bitmap);
}