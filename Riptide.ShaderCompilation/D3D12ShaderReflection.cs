using RiptideEngine.Core;
using Silk.NET.Direct3D12;
using System.Collections.Immutable;
using Buffer = Silk.NET.Direct3D.Compilers.Buffer;

namespace Riptide.ShaderCompilation;

internal sealed unsafe class D3D12ShaderReflection : ShaderReflection {
    private readonly Dictionary<uint, ConstantBufferTypeInfo> _cbTypeInfos = [];
    
    public D3D12ShaderReflection(D3D12CompilationPipeline.DxilReflectInformation pInfo) {
        using ComPtr<ID3D12ShaderReflection> pReflection = default;
        Buffer buffer = new() {
            Ptr = pInfo.Blob->GetBufferPointer(),
            Size = pInfo.Blob->GetBufferSize(),
            Encoding = 0,
        };
        
        int hr = ShaderCompilationEngine.DxcUtils->CreateReflection(&buffer, SilkMarshal.GuidPtrOf<ID3D12ShaderReflection>(), (void**)pReflection.GetAddressOf());
        if (hr < 0) throw new ArgumentException("Failed to create ID3D12ShaderReflection.");

        ShaderDesc sdesc;
        pReflection.GetDesc(&sdesc);

        if (sdesc.ConstantBuffers != 0) {
            var cbbuilder = ImmutableArray.CreateBuilder<ConstantBufferInfo>();
        
            for (uint b = 0; b < sdesc.ConstantBuffers; b++) {
                ShaderInputBindDesc sibdesc;
                ShaderBufferDesc sbdesc;
        
                var cbuffer = pReflection.GetConstantBufferByIndex(b);
                hr = cbuffer->GetDesc(&sbdesc);
                Debug.Assert(hr >= 0, "hr >= 0");
        
                if (sbdesc.Type != D3DCBufferType.D3DCTCbuffer) continue;
        
                hr = pReflection.GetResourceBindingDescByName(sbdesc.Name, &sibdesc);
                Debug.Assert(hr >= 0, "hr >= 0");
                
                var variables = ImmutableArray.CreateBuilder<ConstantBufferVariableInfo>((int)sbdesc.Variables);
                for (uint v = 0; v < sbdesc.Variables; v++) {
                    ShaderVariableDesc vdesc;
                    
                    var variable = cbuffer->GetVariableByIndex(v);
                    hr = variable->GetDesc(&vdesc);
                    Debug.Assert(hr >= 0, "hr >= 0");
                    
                    variables.Add(new(new((sbyte*)vdesc.Name), (ushort)vdesc.StartOffset, (ushort)vdesc.Size, HandleTypeInfoStorage(variable->GetType())));
                }
                
                cbbuilder.Add(new(new((sbyte*)sibdesc.Name), (ushort)sibdesc.BindPoint, (ushort)sibdesc.Space, (ushort)sibdesc.BindCount, (ushort)sbdesc.Size, variables.MoveToImmutable()));
            }
        
            ConstantBuffers = cbbuilder.ToImmutable();
        }
        
        // Resources, Sampler
        {
            var robuilder = ImmutableArray.CreateBuilder<ReadonlyResourceInfo>();
            var uarbuilder = ImmutableArray.CreateBuilder<ReadWriteResourceInfo>();
            var samplerBuilder = ImmutableArray.CreateBuilder<SamplerInfo>();
        
            for (uint r = 0; r < sdesc.BoundResources; r++) {
                ShaderInputBindDesc sibdesc;
                hr = pReflection.GetResourceBindingDesc(r, &sibdesc);
                Debug.Assert(hr >= 0, "hr >= 0");
                
                switch (sibdesc.Type) {
                    case D3DShaderInputType.D3DSitCbuffer: break;
        
                    case D3DShaderInputType.D3DSitTexture:
                        robuilder.Add(new(new((sbyte*)sibdesc.Name), (ushort)sibdesc.BindPoint, (ushort)sibdesc.Space, (ushort)sibdesc.BindCount, sibdesc.Dimension switch {
                            D3DSrvDimension.D3DSrvDimensionBuffer => ReadonlyResourceType.Buffer,
                            D3DSrvDimension.D3DSrvDimensionTexture1D => ReadonlyResourceType.Texture1D,
                            D3DSrvDimension.D3DSrvDimensionTexture1Darray => ReadonlyResourceType.Texture1DArray,
                            D3DSrvDimension.D3DSrvDimensionTexture2D => ReadonlyResourceType.Texture2D,
                            D3DSrvDimension.D3DSrvDimensionTexture2Darray => ReadonlyResourceType.Texture2DArray,
                            D3DSrvDimension.D3DSrvDimensionTexture2Dms => ReadonlyResourceType.Texture2DMS,
                            D3DSrvDimension.D3DSrvDimensionTexture2Dmsarray => ReadonlyResourceType.Texture2DMSArray,
                            D3DSrvDimension.D3DSrvDimensionTexture3D => ReadonlyResourceType.Texture3D,
                            D3DSrvDimension.D3DSrvDimensionTexturecube => ReadonlyResourceType.TextureCube,
                            D3DSrvDimension.D3DSrvDimensionTexturecubearray => ReadonlyResourceType.TextureCubeArray,
                            _ => throw new NotImplementedException($"Unimplemented case '{sibdesc.Dimension}'."),
                        }));
                        break;
                    
                    case D3DShaderInputType.D3DSitStructured:
                        robuilder.Add(new(new((sbyte*)sibdesc.Name), (ushort)sibdesc.BindPoint, (ushort)sibdesc.Space, (ushort)sibdesc.BindCount, ReadonlyResourceType.StructuredBuffer));
                        break;
                    
                    case D3DShaderInputType.D3DSitByteaddress:
                        robuilder.Add(new(new((sbyte*)sibdesc.Name), (ushort)sibdesc.BindPoint, (ushort)sibdesc.Space, (ushort)sibdesc.BindCount, ReadonlyResourceType.ByteAddressBuffer));
                        break;
                    
                    case D3DShaderInputType.D3DSitUavRwtyped:
                        uarbuilder.Add(new(new((sbyte*)sibdesc.Name), (ushort)sibdesc.BindPoint, (ushort)sibdesc.Space, (ushort)sibdesc.BindCount, sibdesc.Dimension switch {
                            D3DSrvDimension.D3DSrvDimensionBuffer => UnorderedAccessResourceType.RWBuffer,
                            D3DSrvDimension.D3DSrvDimensionTexture1D => UnorderedAccessResourceType.RWTexture1D,
                            D3DSrvDimension.D3DSrvDimensionTexture1Darray => UnorderedAccessResourceType.RWTexture1DArray,
                            D3DSrvDimension.D3DSrvDimensionTexture2D => UnorderedAccessResourceType.RWTexture2D,
                            D3DSrvDimension.D3DSrvDimensionTexture2Darray => UnorderedAccessResourceType.RWTexture2DArray,
                            D3DSrvDimension.D3DSrvDimensionTexture3D => UnorderedAccessResourceType.RWTexture3D,
                            _ => throw new NotImplementedException($"Unimplemented case '{sibdesc.Dimension}'."),
                        }));
                        break;
                    
                    case D3DShaderInputType.D3DSitUavRwstructured:
                        uarbuilder.Add(new(new((sbyte*)sibdesc.Name), (ushort)sibdesc.BindPoint, (ushort)sibdesc.Space, (ushort)sibdesc.BindCount, UnorderedAccessResourceType.RWStructuredBuffer));
                        break;
                    
                    case D3DShaderInputType.D3DSitUavRwstructuredWithCounter:
                        uarbuilder.Add(new(new((sbyte*)sibdesc.Name), (ushort)sibdesc.BindPoint, (ushort)sibdesc.Space, (ushort)sibdesc.BindCount, UnorderedAccessResourceType.RWStructuredBufferWithCounter));
                        break;
                    
                    case D3DShaderInputType.D3DSitUavRwbyteaddress:
                        uarbuilder.Add(new(new((sbyte*)sibdesc.Name), (ushort)sibdesc.BindPoint, (ushort)sibdesc.Space, (ushort)sibdesc.BindCount, UnorderedAccessResourceType.RWByteAddressBuffer));
                        break;
                    
                    case D3DShaderInputType.D3DSitUavAppendStructured:
                        uarbuilder.Add(new(new((sbyte*)sibdesc.Name), (ushort)sibdesc.BindPoint, (ushort)sibdesc.Space, (ushort)sibdesc.BindCount, UnorderedAccessResourceType.AppendStructuredBuffer));
                        break;
                    
                    case D3DShaderInputType.D3DSitUavConsumeStructured:
                        uarbuilder.Add(new(new((sbyte*)sibdesc.Name), (ushort)sibdesc.BindPoint, (ushort)sibdesc.Space, (ushort)sibdesc.BindCount, UnorderedAccessResourceType.ConsumeStructuredBuffer));
                        break;
                    
                    case D3DShaderInputType.D3DSitSampler:
                        samplerBuilder.Add(new(new((sbyte*)sibdesc.Name), (ushort)sibdesc.BindPoint, (ushort)sibdesc.Space, (ushort)sibdesc.BindCount));
                        break;
                }
            }
            
            ReadonlyResources = robuilder.ToImmutable();
            ReadWriteResources = uarbuilder.ToImmutable();
            Samplers = samplerBuilder.ToImmutable();
        }

        uint nx, ny, nz;
        pReflection.GetThreadGroupSize(&nx, &ny, &nz);
        NumThreads = new(nx, ny, nz);
        
        _refcount = 1;

        static PrimitiveType ConvertPrimitiveType(D3DShaderVariableType type) {
            return type switch {
                D3DShaderVariableType.D3DSvtBool => PrimitiveType.Boolean,
                D3DShaderVariableType.D3DSvtInt16 => PrimitiveType.Int16,
                D3DShaderVariableType.D3DSvtInt => PrimitiveType.Int32,
                D3DShaderVariableType.D3DSvtInt64 => PrimitiveType.Int64,
                D3DShaderVariableType.D3DSvtUint16 => PrimitiveType.UInt16,
                D3DShaderVariableType.D3DSvtUint => PrimitiveType.UInt32,
                D3DShaderVariableType.D3DSvtUint64 => PrimitiveType.UInt64,
                D3DShaderVariableType.D3DSvtFloat16 => PrimitiveType.Float16,
                D3DShaderVariableType.D3DSvtFloat => PrimitiveType.Float32,
                D3DShaderVariableType.D3DSvtDouble => PrimitiveType.Float64,
                
                _ => throw new UnreachableException($"Unreachable case '{type}'."),
            };
        }
        
        uint HandleTypeInfoStorage(ID3D12ShaderReflectionType* pType) {
            ShaderTypeDesc tdesc;
            int hr = pType->GetDesc(&tdesc);
            Debug.Assert(hr >= 0, "hr >= 0");
        
            var hash = Crc32C.Compute(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(tdesc.Name));
            if (_cbTypeInfos.ContainsKey(hash)) return hash;
        
            var baseType = pType->GetBaseClass();
            uint baseTypeHash = baseType == null ? 0 : HandleTypeInfoStorage(baseType);
        
            var builder = ImmutableArray.CreateBuilder<uint>((int)tdesc.Members);
            for (uint m = 0; m < tdesc.Members; m++) {
                builder.Add(HandleTypeInfoStorage(pType->GetMemberTypeByIndex(m)));
            }
            
            switch (tdesc.Class) {
                case D3DShaderVariableClass.D3DSvcScalar:
                    _cbTypeInfos.Add(hash, new(new((sbyte*)tdesc.Name), hash, baseTypeHash, TypeClass.Scalar, ConvertPrimitiveType(tdesc.Type), (byte)tdesc.Rows, (byte)tdesc.Columns, tdesc.Elements, builder.MoveToImmutable()));
                    break;
                
                case D3DShaderVariableClass.D3DSvcVector:
                    _cbTypeInfos.Add(hash, new(new((sbyte*)tdesc.Name), hash, baseTypeHash, TypeClass.Vector, ConvertPrimitiveType(tdesc.Type), (byte)tdesc.Rows, (byte)tdesc.Columns, tdesc.Elements, builder.MoveToImmutable()));
                    break;
                
                case D3DShaderVariableClass.D3DSvcMatrixRows or D3DShaderVariableClass.D3DSvcMatrixColumns:
                    _cbTypeInfos.Add(hash, new(new((sbyte*)tdesc.Name), hash, baseTypeHash, TypeClass.Matrix, ConvertPrimitiveType(tdesc.Type), (byte)tdesc.Rows, (byte)tdesc.Columns, tdesc.Elements, builder.MoveToImmutable()));
                    break;
                
                case D3DShaderVariableClass.D3DSvcStruct:
                    _cbTypeInfos.Add(hash, new(new((sbyte*)tdesc.Name), hash, baseTypeHash, TypeClass.Matrix, PrimitiveType.Struct, (byte)tdesc.Rows, (byte)tdesc.Columns, tdesc.Elements, builder.MoveToImmutable()));
                    break;
            }
            
            return hash;
        }

    }

    public override ConstantBufferTypeInfo GetConstantBufferVariableType(uint id) => _cbTypeInfos[id];

    protected override void Dispose() { }
}