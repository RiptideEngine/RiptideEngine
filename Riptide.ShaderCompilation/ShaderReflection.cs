using RiptideEngine.Core;
using RiptideMathematics;
using System.Collections.Immutable;

namespace Riptide.ShaderCompilation;

public abstract class ShaderReflection : ReferenceCounted {
    public Vector3UInt NumThreads { get; protected init; }
    
    public ImmutableArray<ConstantBufferInfo> ConstantBuffers { get; protected init; }
    public ImmutableArray<ReadonlyResourceInfo> ReadonlyResources { get; protected init; }
    public ImmutableArray<ReadWriteResourceInfo> ReadWriteResources { get; protected init; }
    public ImmutableArray<SamplerInfo> Samplers { get; protected init; }
    
    public abstract ConstantBufferTypeInfo GetConstantBufferVariableType(uint id);
    
    public enum TypeClass : byte {
        Scalar,
        Vector,
        Matrix,
        Struct,
    }
    public enum PrimitiveType : byte {
        Struct,
        Boolean,
        Int16,
        Int32,
        Int64,
        UInt16,
        UInt32,
        UInt64,
        Float16,
        Float32,
        Float64,
    }

    public readonly record struct ConstantBufferTypeInfo(string Name, uint ID, uint BaseTypeID, TypeClass Class, PrimitiveType Type, byte NumRows, byte NumColumns, uint NumArrayElements, ImmutableArray<uint> MemberTypeIDs);
    
    public readonly record struct ConstantBufferInfo(string Name, ushort Register, ushort Space, ushort Elements, ushort Size, ImmutableArray<ConstantBufferVariableInfo> Variables);
    public readonly record struct ConstantBufferVariableInfo(string Name, ushort Offset, ushort Width, uint TypeID);
    
    public readonly record struct ReadonlyResourceInfo {
        public readonly string Name;
        public readonly ushort Register;
        public readonly ushort Space;
        public readonly ushort Elements;
        public readonly ReadonlyResourceType Type;
        
        internal ReadonlyResourceInfo(string name, ushort register, ushort space, ushort elements, ReadonlyResourceType type) {
            Name = name;
            Register = register;
            Space = space;
            Elements = elements;
            Type = type;
        }
    }

    public readonly record struct ReadWriteResourceInfo {
        public readonly string Name;
        public readonly ushort Register;
        public readonly ushort Space;
        public readonly ushort Elements;
        public readonly UnorderedAccessResourceType Type;
        
        internal ReadWriteResourceInfo(string name, ushort register, ushort space, ushort elements, UnorderedAccessResourceType type) {
            Name = name;
            Register = register;
            Space = space;
            Elements = elements;
            Type = type;
        }
    }
    
    public readonly struct SamplerInfo {
        public readonly string Name;
        public readonly ushort Register;
        public readonly ushort Space;
        public readonly ushort Elements;

        internal SamplerInfo(string name, ushort register, ushort space, ushort elements) {
            Name = name;
            Register = register;
            Space = space;
            Elements = elements;
        }
    }
}