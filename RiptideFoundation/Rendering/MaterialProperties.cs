using Riptide.ShaderCompilation;

namespace RiptideFoundation.Rendering;

public sealed class MaterialProperties : RenderingObject {
    private readonly CompactedShaderReflection _reflection;

    private ResourceSignature _signature;
    private ImmutableArray<ArgumentTable> _argumentTables;
    private ConstantParameterValue[] _constants;
    
    public MaterialProperties(CompactedShaderReflection reflection, ResourceSignature signature) {
        _reflection = reflection;
        
        var builder = ImmutableArray.CreateBuilder<ArgumentTable>();

        var parameters = signature.Parameters;

        uint numConstants = 0;
        for (int i = 0; i < parameters.Length; i++) {
            ref readonly var parameter = ref parameters[i];
            
            switch (parameter.Type) {
                case ResourceParameterType.Constants:
                    numConstants += parameter.Constants.NumConstants;
                    builder.Add(new((byte)i, default));
                    break;
                
                case ResourceParameterType.Descriptors:
                    ref readonly var table = ref parameter.Descriptors;

                    switch (table.Type) {
                        case DescriptorTableType.ConstantBuffer:
                            builder.Add(new((byte)i, new() {
                                Cbvs = new CBV[table.NumDescriptors],
                            }));
                            break;
                        
                        case DescriptorTableType.ShaderResourceView:
                            var srvs = new SRV[table.NumDescriptors];

                            for (uint d = 0; d < table.NumDescriptors; d++) {
                                ShaderResourceViewDimension dimension = ShaderResourceViewDimension.Unknown;
                                var register = table.BaseRegister + d;
                                
                                foreach (ref readonly var info in _reflection.ReadonlyResources.AsSpan()) {
                                    ref readonly var info2 = ref info.Info;

                                    if (info2.Space != table.Space || info2.Register != register) continue;

                                    dimension = info2.Type switch {
                                        ReadonlyResourceType.Buffer or ReadonlyResourceType.StructuredBuffer or ReadonlyResourceType.ByteAddressBuffer => ShaderResourceViewDimension.Buffer,
                                        ReadonlyResourceType.Texture1D => ShaderResourceViewDimension.Texture1D,
                                        ReadonlyResourceType.Texture1DArray => ShaderResourceViewDimension.Texture1DArray,
                                        ReadonlyResourceType.Texture2D => ShaderResourceViewDimension.Texture2D,
                                        ReadonlyResourceType.Texture2DArray => ShaderResourceViewDimension.Texture2DArray,
                                        ReadonlyResourceType.Texture3D => ShaderResourceViewDimension.Texture3D,
                                        ReadonlyResourceType.TextureCube => ShaderResourceViewDimension.TextureCube,
                                        ReadonlyResourceType.TextureCubeArray => ShaderResourceViewDimension.TextureCubeArray,
                                        ReadonlyResourceType.Texture2DMS or ReadonlyResourceType.Texture2DMSArray => throw new NotSupportedException("Texture2DMS and Texture2DMSArray is not currently supported."),
                                        _ => throw new UnreachableException($"Unreachable case '{info2.Type}'."),
                                    };
                                    
                                    break;
                                }
                                
                                srvs[d] = new(dimension);
                            }
                            
                            builder.Add(new((byte)i, new() {
                                Srvs = srvs,
                            }));
                            break;
                        
                        case DescriptorTableType.UnorderedAccessView: throw new NotSupportedException("Root Parameter contains Unordered Access View, which is not supported.");
                    }
                    break;
            }
        }

        _argumentTables = builder.ToImmutable();
        _signature = signature;
        _constants = numConstants == 0 ? [] : new ConstantParameterValue[numConstants];

        _signature.IncrementReference();
        _refcount = 1;
    }

    public void SetConstants(ReadOnlySpan<char> name, ReadOnlySpan<ConstantParameterValue> values, uint offset = 0) {
        if (!_reflection.TryGetConstantBufferInfo(name, out var info)) return;
        
        ref readonly var info2 = ref info.Info;
    
        ref var firstParameter = ref MemoryMarshal.GetReference(_signature.Parameters);
    
        uint constantOffset = 0;
        for (int i = 0; i < _signature.Parameters.Length; i++) {
            ref readonly var parameter = ref Unsafe.Add(ref firstParameter, i);
            if (parameter.Type != ResourceParameterType.Constants) continue;
            
            ref readonly var constants = ref parameter.Constants;
    
            if (info2.Space != constants.Space || info2.Register != constants.Register) {
                constantOffset += constants.NumConstants;
                continue;
            }
    
            var destination = _constants.AsSpan((int)constantOffset, (int)constants.NumConstants);
    
            if (offset >= destination.Length) break;
            
            int copyAmount = int.Min(values.Length, (int)(constants.NumConstants - offset));
            values[..copyAmount].CopyTo(destination[(int)offset..]);
            
            break;
        }
    }

    public void SetConstantBuffer(ReadOnlySpan<char> name, GpuBuffer? buffer, uint offset, uint size) => SetConstantBuffer(name, 0, buffer, offset, size);
    public void SetConstantBuffer(ReadOnlySpan<char> name, uint elementOffset, GpuBuffer? buffer, uint offset, uint size) {
        if (!_reflection.TryGetConstantBufferInfo(name, out var info)) return;
        
        ref readonly var info2 = ref info.Info;

        (var table, var index) = SearchDescriptorFromReflection(elementOffset, DescriptorTableType.ConstantBuffer, info2.Register, info2.Space, info2.Elements);
        
        Debug.Assert(table != uint.MaxValue && index != uint.MaxValue, "table != uint.MaxValue && index != uint.MaxValue");

        ref var cbv = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(ImmutableCollectionsMarshal.AsArray(_argumentTables)!), table).RootArgument.Cbvs[index];
        cbv.Buffer?.DecrementReference();
        
        if (buffer == null || size == 0) {
            cbv = default;
        } else {
            cbv = new(buffer, offset, size);
            buffer.IncrementReference();
        }
    }

    public void GetConstantBuffer(ReadOnlySpan<char> name, out GpuBuffer? buffer, out uint offset, out uint size) => GetConstantBuffer(name, 0, out buffer, out offset, out size);
    public void GetConstantBuffer(ReadOnlySpan<char> name, uint elementOffset, out GpuBuffer? buffer, out uint offset, out uint size) {
        if (!_reflection.TryGetConstantBufferInfo(name, out var info)) {
            buffer = null;
            offset = size = 0;
            return;
        }
        
        ref readonly var info2 = ref info.Info;

        (var table, var index) = SearchDescriptorFromReflection(elementOffset, DescriptorTableType.ConstantBuffer, info2.Register, info2.Space, info2.Elements);
        
        Debug.Assert(table != uint.MaxValue && index != uint.MaxValue, "table != uint.MaxValue && index != uint.MaxValue");

        ref var cbv = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(ImmutableCollectionsMarshal.AsArray(_argumentTables)!), table).RootArgument.Cbvs[index];

        buffer = cbv.Buffer;
        offset = cbv.Offset;
        size = cbv.Size;
    }

    public void SetBuffer(ReadOnlySpan<char> name, Buffer? buffer) => SetBuffer(name, 0, buffer);
    public void SetBuffer(ReadOnlySpan<char> name, uint elementOffset, Buffer? buffer) {
        if (!_reflection.TryGetReadonlyResourceInfo(name, out var info)) return;
        
        ref readonly var info2 = ref info.Info;

        (var table, var index) = SearchDescriptorFromReflection(elementOffset, DescriptorTableType.ShaderResourceView, info2.Register, info2.Space, info2.Elements);
        
        Debug.Assert(table != uint.MaxValue && index != uint.MaxValue, "table != uint.MaxValue && index != uint.MaxValue");
        
        ref var srv = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(ImmutableCollectionsMarshal.AsArray(_argumentTables)!), table).RootArgument.Srvs[index];
        if (srv.Dimension != ShaderResourceViewDimension.Buffer) return;
        
        srv.Resource.Buffer?.DecrementReference();
        srv.Resource = buffer;
        buffer?.IncrementReference();
    }

    public Buffer? GetBuffer(ReadOnlySpan<char> name, uint elementOffset = 0) {
        if (!_reflection.TryGetReadonlyResourceInfo(name, out var info)) return null;
        
        ref readonly var info2 = ref info.Info;

        (var table, var index) = SearchDescriptorFromReflection(elementOffset, DescriptorTableType.ShaderResourceView, info2.Register, info2.Space, info2.Elements);
        
        Debug.Assert(table != uint.MaxValue && index != uint.MaxValue, "table != uint.MaxValue && index != uint.MaxValue");
        
        ref var srv = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(ImmutableCollectionsMarshal.AsArray(_argumentTables)!), table).RootArgument.Srvs[index];
        
        return srv.Dimension != ShaderResourceViewDimension.Buffer ? null : srv.Resource.Buffer;
    }
    
    public void SetTexture(ReadOnlySpan<char> name, Texture? texture) => SetTexture(name, 0, texture);
    public void SetTexture(ReadOnlySpan<char> name, uint elementOffset, Texture? texture) {
        if (!_reflection.TryGetReadonlyResourceInfo(name, out var info)) return;
        
        ref readonly var info2 = ref info.Info;

        (var table, var index) = SearchDescriptorFromReflection(elementOffset, DescriptorTableType.ShaderResourceView, info2.Register, info2.Space, info2.Elements);
        
        Debug.Assert(table != uint.MaxValue && index != uint.MaxValue, "table != uint.MaxValue && index != uint.MaxValue");
        
        ref var srv = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(ImmutableCollectionsMarshal.AsArray(_argumentTables)!), table).RootArgument.Srvs[index];

        if (texture == null) {
            srv.Resource.Texture?.DecrementReference();
            srv.Resource = default;
        } else {
            if (texture.SrvDimension != srv.Dimension) return;

            srv.Resource.Texture?.DecrementReference();
            srv.Resource = texture;
            texture.IncrementReference();
        }
    }

    public Texture? GetTexture(ReadOnlySpan<char> name, uint elementOffset = 0) {
        if (!_reflection.TryGetReadonlyResourceInfo(name, out var info)) return null;
        
        ref readonly var info2 = ref info.Info;

        (var table, var index) = SearchDescriptorFromReflection(elementOffset, DescriptorTableType.ShaderResourceView, info2.Register, info2.Space, info2.Elements);
        
        Debug.Assert(table != uint.MaxValue && index != uint.MaxValue, "table != uint.MaxValue && index != uint.MaxValue");
        
        ref var srv = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(ImmutableCollectionsMarshal.AsArray(_argumentTables)!), table).RootArgument.Srvs[index];

        return srv.Dimension == ShaderResourceViewDimension.Buffer ? null : srv.Resource.Texture;
    }

    private (uint ParameterIndex, uint Offset) SearchDescriptorFromReflection(uint elementOffset, DescriptorTableType typeFilter, uint infoRegister, uint infoSpace, uint infoNumElements) {
        ref var firstParameter = ref MemoryMarshal.GetReference(_signature.Parameters);

        for (int i = 0; i < _signature.Parameters.Length; i++) {
            ref readonly var parameter = ref Unsafe.Add(ref firstParameter, i);
                
            if (parameter.Type != ResourceParameterType.Descriptors) continue;
                
            ref readonly var descriptors = ref parameter.Descriptors;

            if (descriptors.Type != typeFilter) continue;
            if (infoSpace != descriptors.Space) continue;
            if (elementOffset >= infoNumElements) continue;
                
            bool overlap = infoRegister < descriptors.BaseRegister + descriptors.NumDescriptors && infoRegister + infoNumElements > descriptors.BaseRegister;
            if (!overlap) continue;

            return ((uint)i, infoRegister - descriptors.BaseRegister + elementOffset);
        }

        return (uint.MaxValue, uint.MaxValue);
    }

    public void BindGraphics(CommandList cmdList) {
        ref var firstParameter = ref MemoryMarshal.GetReference(_signature.Parameters);
        ref var firstTable = ref MemoryMarshal.GetArrayDataReference(ImmutableCollectionsMarshal.AsArray(_argumentTables)!);

        uint constantOffset = 0;
        for (int i = 0; i < _signature.Parameters.Length; i++) {
            ref readonly var parameter = ref Unsafe.Add(ref firstParameter, i);
            ref readonly var argTable = ref Unsafe.Add(ref firstTable, i);

            switch (parameter.Type) {
                case ResourceParameterType.Constants:
                    var constants = parameter.Constants;
                    
                    cmdList.SetGraphicsConstants((uint)i, _constants.AsSpan((int)constantOffset, (int)constants.NumConstants), 0);
                    constantOffset += constants.NumConstants;
                    break;
                
                case ResourceParameterType.Descriptors:
                    ref readonly var descriptors = ref parameter.Descriptors;

                    switch (descriptors.Type) {
                        case DescriptorTableType.ConstantBuffer:
                            var cbvs = argTable.RootArgument.Cbvs;
                            
                            Debug.Assert(descriptors.NumDescriptors == cbvs.Length, "descriptors.NumDescriptors == arguments.Length");

                            for (uint a = 0; a < descriptors.NumDescriptors; a++) {
                                var cbv = cbvs[a];
                                
                                cmdList.SetGraphicsConstantBufferView((uint)i, a, cbv.Buffer, cbv.Offset, cbv.Size);
                            }
                            break;
                        
                        case DescriptorTableType.ShaderResourceView:
                            var srvs = argTable.RootArgument.Srvs;
                            
                            Debug.Assert(descriptors.NumDescriptors == srvs.Length, "descriptors.NumDescriptors == srvs.Length");

                            for (uint a = 0; a < descriptors.NumDescriptors; a++) {
                                var srv = srvs[a];

                                if (srv.Resource.Buffer == null) {
                                    cmdList.NullifyGraphicsShaderResourceView((uint)i, a, srv.Dimension);
                                } else {
                                    cmdList.SetGraphicsShaderResourceView((uint)i, a, srv.Dimension switch {
                                        ShaderResourceViewDimension.Buffer => srv.Resource.Buffer.UnderlyingSrv,
                                        _ => srv.Resource.Texture!.UnderlyingSrv,
                                    });
                                }
                            }
                            break;
                    }
                    break;
            }
        }
    }

    private void DisposeProperties() {
        ref var firstParameter = ref MemoryMarshal.GetReference(_signature.Parameters);
        ref var firstTable = ref MemoryMarshal.GetArrayDataReference(ImmutableCollectionsMarshal.AsArray(_argumentTables)!);
        
        for (int i = 0; i < _signature.Parameters.Length; i++) {
            ref readonly var parameter = ref Unsafe.Add(ref firstParameter, i);
            ref readonly var argTable = ref Unsafe.Add(ref firstTable, i);

            switch (parameter.Type) {
                case ResourceParameterType.Constants: break;
                case ResourceParameterType.Descriptors:
                    ref readonly var descriptors = ref parameter.Descriptors;

                    switch (descriptors.Type) {
                        case DescriptorTableType.ConstantBuffer: {
                            var cbvs = argTable.RootArgument.Cbvs;
                            
                            for (uint a = 0; a < descriptors.NumDescriptors; a++) {
                                cbvs[a].Buffer?.DecrementReference();
                            }
                            break;
                        }
                        
                        case DescriptorTableType.ShaderResourceView:
                            var srvs = argTable.RootArgument.Srvs;

                            for (uint a = 0; a < descriptors.NumDescriptors; a++) {
                                srvs[a].Resource.Buffer?.DecrementReference();
                            }
                            break;
                    }
                    break;
            }
        }

        _argumentTables = [];
        _constants = [];
    }

    protected override void Dispose() {
        DisposeProperties();
        _signature.DecrementReference();
        _signature = null!;
    }

    private readonly record struct ArgumentTable(byte ParameterIndex, RootArgument RootArgument);

    private readonly record struct CBV(GpuBuffer? Buffer, uint Offset, uint Size);
    private struct SRV(ShaderResourceViewDimension dimension) {
        public readonly ShaderResourceViewDimension Dimension = dimension;
        public SRVResource Resource;
    }
    
    [StructLayout(LayoutKind.Explicit)]
    private struct RootArgument {
        [FieldOffset(0)] public CBV[] Cbvs;
        [FieldOffset(0)] public SRV[] Srvs;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct SRVResource {
        [FieldOffset(0)] public Buffer? Buffer;
        [FieldOffset(0)] public Texture? Texture;

        public static implicit operator SRVResource(Buffer? buffer) => new() {
            Buffer = buffer,
        };
        
        public static implicit operator SRVResource(Texture? texture) => new() {
            Texture = texture,
        };
    }
}