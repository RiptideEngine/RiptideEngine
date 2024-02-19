namespace RiptideFoundation;

partial class CompactedShaderReflection {
    public bool TryGetConstantBufferInfo(ReadOnlySpan<char> name, out ConstantBufferInfo output) {
        foreach (ref readonly var info in ConstantBuffers.AsSpan()) {
            if (name.SequenceEqual(info.Info.Name)) {
                output = info;
                return true;
            }
        }

        output = default;
        return false;
    }
    
    public bool TryGetReadonlyResourceInfo(ReadOnlySpan<char> name, out ReadonlyResourceInfo output) {
        foreach (ref readonly var info in ReadonlyResources.AsSpan()) {
            if (name.SequenceEqual(info.Info.Name)) {
                output = info;
                return true;
            }
        }

        output = default;
        return false;
    }
    
    public bool TryGetReadWriteResourceInfo(ReadOnlySpan<char> name, out ReadWriteResourceInfo output) {
        foreach (ref readonly var info in ReadWriteResources.AsSpan()) {
            if (name.SequenceEqual(info.Info.Name)) {
                output = info;
                return true;
            }
        }

        output = default;
        return false;
    }
    
    public bool TryGetSamplerInfo(ReadOnlySpan<char> name, out SamplerInfo output) {
        foreach (ref readonly var info in Samplers.AsSpan()) {
            if (name.SequenceEqual(info.Info.Name)) {
                output = info;
                return true;
            }
        }

        output = default;
        return false;
    }
}