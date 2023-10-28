namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12ShaderReflector : ShaderReflector {
    private struct ConstantBufferCache {
        public uint Size;
    }
    private struct ShaderResourceViewCache {
        public ResourceType Type;
    }
    private struct UnorderedAccessViewCache {
        public ResourceType Type;
    }

    public D3D12ShaderReflector(ID3D12ShaderReflection* pCSReflector, in RootSignatureDesc rootSignatureDesc) {
        ShaderDesc sdesc;
        int hr = pCSReflector->GetDesc(&sdesc);
        Debug.Assert(hr >= 0);

        if (sdesc.ConstantBuffers == 0) {
            ConstantBufferInfos = ImmutableArray<ConstantBufferInfo>.Empty;
        } else {
            var builder = ImmutableArray.CreateBuilder<ConstantBufferInfo>((int)sdesc.ConstantBuffers);

            for (uint cb = 0; cb < sdesc.ConstantBuffers; cb++) {
                var reflector = pCSReflector->GetConstantBufferByIndex(cb);
                ShaderBufferDesc sbdesc;
                hr = reflector->GetDesc(&sbdesc);
                Debug.Assert(hr >= 0);

                ShaderInputBindDesc sibdesc;
                hr = pCSReflector->GetResourceBindingDescByName(sbdesc.Name, &sibdesc);
                Debug.Assert(hr >= 0);

                builder.Add(new(new string((sbyte*)sibdesc.Name), new(sibdesc.BindPoint, sibdesc.Space), sbdesc.Size, D3D12Utils.CheckIsRootConstant(rootSignatureDesc.PParameters, rootSignatureDesc.NumParameters, sibdesc.BindPoint, sibdesc.Space)));
            }

            ConstantBufferInfos = builder.MoveToImmutable();
        }

        var srvbuilder = ImmutableArray.CreateBuilder<ReadonlyResourceInfo>();
        var uavbuilder = ImmutableArray.CreateBuilder<ReadWriteResourceInfo>();

        {
            ShaderInputBindDesc sibdesc;
            for (uint r = 0; pCSReflector->GetResourceBindingDesc(r, &sibdesc) >= 0; r++) {
                var name = new string((sbyte*)sibdesc.Name);
                var loc = new ResourceBindLocation(sibdesc.BindPoint, sibdesc.Space);

                switch (sibdesc.Type) {
                    case D3DShaderInputType.D3DSitTbuffer:
                        srvbuilder.Add(new(name, loc, ResourceType.TextureBuffer));
                        break;

                    case D3DShaderInputType.D3DSitTexture:
                        srvbuilder.Add(new(name, loc, sibdesc.Dimension switch {
                            D3DSrvDimension.D3DSrvDimensionTexture1D => ResourceType.Texture1D,
                            D3DSrvDimension.D3DSrvDimensionTexture1Darray => ResourceType.Texture1DArray,
                            D3DSrvDimension.D3DSrvDimensionTexture2D => ResourceType.Texture2D,
                            D3DSrvDimension.D3DSrvDimensionTexture2Darray => ResourceType.Texture2DArray,
                            D3DSrvDimension.D3DSrvDimensionTexture2Dms => ResourceType.Texture2DMS,
                            D3DSrvDimension.D3DSrvDimensionTexture2Dmsarray => ResourceType.Texture2DMSArray,
                            D3DSrvDimension.D3DSrvDimensionTexture3D => ResourceType.Texture3D,
                            D3DSrvDimension.D3DSrvDimensionTexturecube => ResourceType.TextureCube,
                            D3DSrvDimension.D3DSrvDimensionTexturecubearray => ResourceType.TextureCubeArray,

                            D3DSrvDimension.D3DSrvDimensionBuffer => ResourceType.Buffer,
                            D3DSrvDimension.D3DSrvDimensionBufferex => throw new NotImplementedException("BufferEx."),
                            D3DSrvDimension.D3DSrvDimensionUnknown or _ => throw new UnreachableException("Unknown/Undefined."),
                        }));
                        break;

                    case D3DShaderInputType.D3DSitSampler: break;

                    case D3DShaderInputType.D3DSitUavRwtyped:
                        uavbuilder.Add(new(name, loc, sibdesc.Dimension switch {
                            D3DSrvDimension.D3DSrvDimensionTexture1D => ResourceType.RWTexture1D,
                            D3DSrvDimension.D3DSrvDimensionTexture1Darray => ResourceType.RWTexture1DArray,
                            D3DSrvDimension.D3DSrvDimensionTexture2D => ResourceType.RWTexture2D,
                            D3DSrvDimension.D3DSrvDimensionTexture2Darray => ResourceType.RWTexture2DArray,
                            D3DSrvDimension.D3DSrvDimensionTexture3D => ResourceType.RWTexture3D,

                            D3DSrvDimension.D3DSrvDimensionBuffer => ResourceType.RWBuffer,
                            D3DSrvDimension.D3DSrvDimensionBufferex => throw new NotImplementedException("BufferEx."),
                            D3DSrvDimension.D3DSrvDimensionUnknown or _ => throw new UnreachableException("Unknown/Undefined."),
                        }));
                        break;

                    case D3DShaderInputType.D3DSitStructured:
                        srvbuilder.Add(new(name, loc, ResourceType.StructuredBuffer));
                        break;

                    case D3DShaderInputType.D3DSitUavRwstructured:
                        uavbuilder.Add(new(name, loc, ResourceType.RWStructuredBuffer));
                        break;

                    case D3DShaderInputType.D3DSitByteaddress:
                        srvbuilder.Add(new(name, loc, ResourceType.ByteAddressBuffer));
                        break;

                    case D3DShaderInputType.D3DSitUavRwbyteaddress:
                        uavbuilder.Add(new(name, loc, ResourceType.RWByteAddressBuffer));
                        break;

                    case D3DShaderInputType.D3DSitUavAppendStructured:
                        uavbuilder.Add(new(name, loc, ResourceType.AppendStructuredBuffer));
                        break;

                    case D3DShaderInputType.D3DSitUavConsumeStructured:
                        uavbuilder.Add(new(name, loc, ResourceType.ConsumeStructuredBuffer));
                        break;

                    case D3DShaderInputType.D3DSitUavRwstructuredWithCounter:
                        uavbuilder.Add(new(name, loc, ResourceType.RWStructuredBufferWithCounter));
                        break;
                }
            }
        }
        ReadonlyResourceInfos = srvbuilder.DrainToImmutable();
        ReadWriteResourceInfos = uavbuilder.DrainToImmutable();

        uint x, y, z;
        pCSReflector->GetThreadGroupSize(&x, &y, &z);

        ComputeThreadSize = (x, y, z);
    }

    public D3D12ShaderReflector(ID3D12ShaderReflection** ppReflectors, uint numReflectors, in RootSignatureDesc rootSigDesc) {
        Debug.Assert(numReflectors is >= 1 and <= 5);

        int hr;
        ShaderDesc* sdescs = stackalloc ShaderDesc[(int)numReflectors];
        for (uint i = 0; i < numReflectors; i++) {
            hr = ppReflectors[i]->GetDesc(sdescs + i);
            Debug.Assert(hr >= 0);
        }

        Dictionary<uint, ConstantBufferCache> cbcache = new();
        Dictionary<uint, ShaderResourceViewCache> srvcache = new();
        Dictionary<uint, UnorderedAccessViewCache> uavcache = new();
        // Sampler cache here

        for (uint i = 0; i < numReflectors; i++) {
            ID3D12ShaderReflection* pReflection = ppReflectors[i];
            ref ShaderDesc sdesc = ref sdescs[i];

            ShaderInputBindDesc sibdesc;
            for (uint r = 0; pReflection->GetResourceBindingDesc(r, &sibdesc) >= 0; r++) {
                var nameHash = Crc32C.Compute(sibdesc.Name, UnsafeHelpers.StringLength(sibdesc.Name));

                switch (sibdesc.Type) {
                    case D3DShaderInputType.D3DSitSampler:
                        break;

                    case D3DShaderInputType.D3DSitCbuffer: {
                        var bufferReflection = pReflection->GetConstantBufferByName(sibdesc.Name);

                        ShaderBufferDesc sbdesc;
                        hr = bufferReflection->GetDesc(&sbdesc);
                        Marshal.ThrowExceptionForHR(hr);

                        if (cbcache.TryGetValue(nameHash, out var cache)) {
                            if (cache.Size != sbdesc.Size) {
                                throw new Exception($"Constant buffer '{new string((sbyte*)sbdesc.Name)}' is already registered with size of {cache.Size} bytes.");
                            }
                            continue;
                        }

                        cbcache.Add(nameHash, new ConstantBufferCache() {
                            Size = sbdesc.Size,
                        });
                        break;
                    }

                    case D3DShaderInputType.D3DSitTbuffer or D3DShaderInputType.D3DSitTexture or D3DShaderInputType.D3DSitStructured or D3DShaderInputType.D3DSitByteaddress: {
                        if (!D3D12Convert.TryConvert(sibdesc.Type, sibdesc.Dimension, out var resourceType)) {
                            throw new NotSupportedException($"Cannot convert the type of resource '{new string((sbyte*)sibdesc.Name)}' {SilkHelper.GetNativeName(sibdesc.Type, "Name")} to it's correspond ResourceType enum.");
                        }

                        if (srvcache.TryGetValue(nameHash, out var cache)) {
                            if (cache.Type != resourceType) {
                                throw new Exception($"Readonly resource '{new string((sbyte*)sibdesc.Name)}' is already registered as a '{cache.Type}'.");
                            }

                            continue;
                        }

                        srvcache.Add(nameHash, new ShaderResourceViewCache() {
                            Type = resourceType,
                        });
                        break;
                    }

                    case D3DShaderInputType.D3DSitUavRwtyped or D3DShaderInputType.D3DSitUavRwstructured or D3DShaderInputType.D3DSitUavRwbyteaddress or D3DShaderInputType.D3DSitUavAppendStructured or D3DShaderInputType.D3DSitUavConsumeStructured or D3DShaderInputType.D3DSitUavRwstructuredWithCounter: {
                        if (!D3D12Convert.TryConvert(sibdesc.Type, sibdesc.Dimension, out var resourceType)) {
                            throw new NotSupportedException($"Cannot convert the type of resource '{new string((sbyte*)sibdesc.Name)}' {SilkHelper.GetNativeName(sibdesc.Type, "Name")} to it's correspond ResourceType enum.");
                        }

                        if (uavcache.TryGetValue(nameHash, out var cache)) {
                            if (cache.Type != resourceType) {
                                throw new Exception($"ReadWrite resource '{new string((sbyte*)sibdesc.Name)}' is already registered as a '{cache.Type}'.");
                            }

                            continue;
                        }

                        uavcache.Add(nameHash, new UnorderedAccessViewCache() {
                            Type = resourceType,
                        });
                        break;
                    }
                }
            }
        }

        var cbbuilder = ImmutableArray.CreateBuilder<ConstantBufferInfo>(cbcache.Count);
        var srvbuilder = ImmutableArray.CreateBuilder<ReadonlyResourceInfo>(srvcache.Count);
        var uavbuilder = ImmutableArray.CreateBuilder<ReadWriteResourceInfo>(uavcache.Count);

        for (uint i = 0; i < numReflectors; i++) {
            ID3D12ShaderReflection* pReflection = ppReflectors[i];
            ref ShaderDesc sdesc = ref sdescs[i];

            ShaderInputBindDesc sibdesc;
            for (uint r = 0; pReflection->GetResourceBindingDesc(r, &sibdesc) >= 0; r++) {
                var nameHash = Crc32C.Compute(sibdesc.Name, UnsafeHelpers.StringLength(sibdesc.Name));
                var location = new ResourceBindLocation(sibdesc.BindPoint, sibdesc.Space);

                if (cbcache.Remove(nameHash, out var cbcached)) {
                    cbbuilder.Add(new ConstantBufferInfo(new string((sbyte*)sibdesc.Name), location, cbcached.Size, D3D12Utils.CheckIsRootConstant(rootSigDesc.PParameters, rootSigDesc.NumParameters, sibdesc.BindPoint, sibdesc.Space)));
                } else if (srvcache.Remove(nameHash, out var srvcached)) {
                    srvbuilder.Add(new ReadonlyResourceInfo(new string((sbyte*)sibdesc.Name), location, srvcached.Type));
                } else if (uavcache.Remove(nameHash, out var uavcached)) {
                    uavbuilder.Add(new ReadWriteResourceInfo(new string((sbyte*)sibdesc.Name), location, uavcached.Type));
                }
            }
        }

        ConstantBufferInfos = cbbuilder.DrainToImmutable();
        ReadonlyResourceInfos = srvbuilder.DrainToImmutable();
        ReadWriteResourceInfos = uavbuilder.DrainToImmutable();
    }
}