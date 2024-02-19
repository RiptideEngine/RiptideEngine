using Riptide.ShaderCompilation;

namespace RiptideFoundation;

public sealed partial class CompactedShaderReflection {
    public ImmutableArray<ConstantBufferInfo> ConstantBuffers { get; init; }
    public ImmutableArray<ReadonlyResourceInfo> ReadonlyResources { get; init; }
    public ImmutableArray<ReadWriteResourceInfo> ReadWriteResources { get; init; }
    public ImmutableArray<SamplerInfo> Samplers { get; init; }
    
    public CompactedShaderReflection(ShaderReflection vertex, ShaderReflection pixel) : this([
        new(ReflectionStage.Vertex, vertex),
        new(ReflectionStage.Pixel, pixel),
    ]) {}
    
    public CompactedShaderReflection(ShaderReflection vertex, ShaderReflection hull, ShaderReflection domain, ShaderReflection pixel) : this([
        new(ReflectionStage.Vertex, vertex),
        new(ReflectionStage.Hull, hull),
        new(ReflectionStage.Domain, domain),
        new(ReflectionStage.Pixel, pixel),
    ]) {}
    
    public CompactedShaderReflection(ShaderReflection compute) : this([
        new(ReflectionStage.Compute, compute),
    ]) {}
    
    private CompactedShaderReflection(ReadOnlySpan<(ReflectionStage Stage, ShaderReflection Reflection)> reflections) {
        Dictionary<string, (ResourceVisibleStages Stages, ShaderReflection.ConstantBufferInfo Info)> cbinfos = [];
        Dictionary<string, (ResourceVisibleStages Stages, ShaderReflection.ReadonlyResourceInfo Info)> roinfos = [];
        Dictionary<string, (ResourceVisibleStages Stages, ShaderReflection.ReadWriteResourceInfo Info)> uarinfos = [];
        Dictionary<string, (ResourceVisibleStages Stages, ShaderReflection.SamplerInfo Info)> samplerinfos = [];

        foreach ((var stage, var reflection) in reflections) {
            foreach (ref readonly var info in reflection.ConstantBuffers.AsSpan()) {
                ref var values = ref CollectionsMarshal.GetValueRefOrAddDefault(cbinfos, info.Name, out bool exists);

                if (exists) {
                    values.Stages |= (ResourceVisibleStages)(1 << (int)stage);
                } else {
                    values = new((ResourceVisibleStages)(1 << (int)stage), info);
                }
            }
            
            foreach (ref readonly var info in reflection.ReadonlyResources.AsSpan()) {
                ref var values = ref CollectionsMarshal.GetValueRefOrAddDefault(roinfos, info.Name, out bool exists);

                if (exists) {
                    values.Stages |= (ResourceVisibleStages)(1 << (int)stage);
                } else {
                    values = new((ResourceVisibleStages)(1 << (int)stage), info);
                }
            }
            
            foreach (ref readonly var info in reflection.ReadWriteResources.AsSpan()) {
                ref var values = ref CollectionsMarshal.GetValueRefOrAddDefault(uarinfos, info.Name, out bool exists);

                if (exists) {
                    values.Stages |= (ResourceVisibleStages)(1 << (int)stage);
                } else {
                    values = new((ResourceVisibleStages)(1 << (int)stage), info);
                }
            }
            
            foreach (ref readonly var info in reflection.Samplers.AsSpan()) {
                ref var values = ref CollectionsMarshal.GetValueRefOrAddDefault(samplerinfos, info.Name, out bool exists);

                if (exists) {
                    values.Stages |= (ResourceVisibleStages)(1 << (int)stage);
                } else {
                    values = new((ResourceVisibleStages)(1 << (int)stage), info);
                }
            }
        }

        ConstantBuffers = cbinfos.Values.Select(tuple => new ConstantBufferInfo(tuple.Stages, tuple.Info)).ToImmutableArray();
        ReadonlyResources = roinfos.Values.Select(tuple => new ReadonlyResourceInfo(tuple.Stages, tuple.Info)).ToImmutableArray();
        ReadWriteResources = uarinfos.Values.Select(tuple => new ReadWriteResourceInfo(tuple.Stages, tuple.Info)).ToImmutableArray();
        Samplers = samplerinfos.Values.Select(tuple => new SamplerInfo(tuple.Stages, tuple.Info)).ToImmutableArray();
    }
    
    public readonly struct ConstantBufferInfo {
        public readonly ResourceVisibleStages Stages;
        public readonly ShaderReflection.ConstantBufferInfo Info;

        internal ConstantBufferInfo(ResourceVisibleStages stages, ShaderReflection.ConstantBufferInfo info) {
            Stages = stages;
            Info = info;
        }
    }
    public readonly record struct ReadonlyResourceInfo {
        public readonly ResourceVisibleStages Stages;
        public readonly ShaderReflection.ReadonlyResourceInfo Info;
        
        internal ReadonlyResourceInfo(ResourceVisibleStages stages, ShaderReflection.ReadonlyResourceInfo info) {
            Stages = stages;
            Info = info;
        }
    }
    public readonly record struct ReadWriteResourceInfo {
        public readonly ResourceVisibleStages Stages;
        public readonly ShaderReflection.ReadWriteResourceInfo Info;

        internal ReadWriteResourceInfo(ResourceVisibleStages stages, ShaderReflection.ReadWriteResourceInfo info) {
            Stages = stages;
            Info = info;
        }
    }
    public readonly record struct SamplerInfo {
        public readonly ResourceVisibleStages Stages;
        public readonly ShaderReflection.SamplerInfo Info;
        
        internal SamplerInfo(ResourceVisibleStages stages, ShaderReflection.SamplerInfo info) {
            Stages = stages;
            Info = info;
        }
    }
    
    private enum ReflectionStage {
        Vertex = 0,
        Hull = 1,
        Domain = 2,
        // Geometry = 3,
        Pixel = 4,
        Compute = 5,
        //Amplification = 6,
        //Mesh = 7,
    }
    
    [Flags]
    public enum ResourceVisibleStages {
        Vertex = 1 << ReflectionStage.Vertex,
        Hull = 1 << ReflectionStage.Hull,
        Domain = 1 << ReflectionStage.Domain,
        // Geometry = 1 << ReflectionStage.Geometry,
        Pixel = 1 << ReflectionStage.Pixel,
        
        Compute = 1 << ReflectionStage.Compute,
        
        //Amplification = 1 << ReflectionStage.Amplification,
        //Mesh = 1 << ReflectionStage.Mesh,
    }
}