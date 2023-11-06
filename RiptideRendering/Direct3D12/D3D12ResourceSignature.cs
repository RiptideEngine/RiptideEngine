namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12ResourceSignature : ResourceSignature {
    private ComPtr<ID3D12RootSignature> pRoot;
    private RootParameter[] _rootParameters;

    public ID3D12RootSignature* RootSignature => pRoot;
    public ReadOnlySpan<RootParameter> RootParameters => _rootParameters;

    public D3D12ResourceSignature(D3D12RenderingContext context, ReadOnlySpan<ResourceTableDescriptor> resourceTables, ReadOnlySpan<ImmutableSamplerDescriptor> immutableSamplers) {
        int numRanges = 0;
        foreach (ref readonly var table in resourceTables) {
            numRanges += table.Table.Length;
        }

        DescriptorRange[] ranges = ArrayPool<DescriptorRange>.Shared.Rent(numRanges);
        StaticSamplerDesc[] samplers = ArrayPool<StaticSamplerDesc>.Shared.Rent(immutableSamplers.Length);

        try {
            for (uint s = 0; s < immutableSamplers.Length; s++) {
                ref readonly var input = ref immutableSamplers[(int)s];

                samplers[(int)s] = new() {
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

            int rangeIdx = 0;
            for (int t = 0; t < resourceTables.Length; t++) {
                ref readonly var table = ref resourceTables[t];

                for (int r = 0; r < table.Table.Length; r++) {
                    ref readonly var input = ref table.Table[r];

                    ranges[rangeIdx++] = new() {
                        BaseShaderRegister = input.BaseRegister,
                        RegisterSpace = input.Space,
                        NumDescriptors = input.NumResources,
                        OffsetInDescriptorsFromTableStart = uint.MaxValue,
                        RangeType = D3D12Convert.TryConvert(input.Type, out var type) ? type : throw new ArgumentException($"Failed to convert {nameof(ResourceRangeType)} of resource table {t}, range {r}."),
                    };
                }
            }

            fixed (DescriptorRange* pRanges = ranges) {
                fixed (StaticSamplerDesc* pSamplers = samplers) {
                    _rootParameters = new RootParameter[resourceTables.Length];

                    uint rangeOffset = 0;
                    for (int i = 0; i < resourceTables.Length; i++) {
                        _rootParameters[i] = new() {
                            ShaderVisibility = ShaderVisibility.All,
                            ParameterType = RootParameterType.TypeDescriptorTable,
                            DescriptorTable = new() {
                                PDescriptorRanges = pRanges + rangeOffset,
                                NumDescriptorRanges = (uint)resourceTables[i].Table.Length,
                            },
                        };

                        rangeOffset += (uint)resourceTables[i].Table.Length;
                    }

                    fixed (RootParameter* pParams = _rootParameters) {
                        RootSignatureDesc rdesc = new() {
                            Flags = RootSignatureFlags.None,
                            NumParameters = (uint)resourceTables.Length,
                            PParameters = pParams,
                            NumStaticSamplers = (uint)samplers.Length,
                            PStaticSamplers = pSamplers,
                        };

                        using ComPtr<ID3D12RootSignature> pOutputRootSig = default;
                        using ComPtr<ID3D12VersionedRootSignatureDeserializer> pOutputDeserializer = default;

                        context.RootSigStorage.Get(rdesc, pRoot.GetAddressOf());
                    }
                }
            }

        } finally {
            ArrayPool<DescriptorRange>.Shared.Return(ranges);
            ArrayPool<StaticSamplerDesc>.Shared.Return(samplers);
        }

        _refcount = 1;
    }

    protected override void Dispose() {
        pRoot = default;
    }
}