namespace RiptideRendering.ShaderReflection;

public abstract class ShaderReflector {
    public ImmutableArray<ConstantBufferInfo> ConstantBufferInfos { get; protected set; }
    public ImmutableArray<ReadonlyResourceInfo> ReadonlyResourceInfos { get; protected set; }
    public ImmutableArray<ReadWriteResourceInfo> ReadWriteResourceInfos { get; protected set; }
    public ImmutableArray<SamplerInfo> SamplerInfos { get; protected set; }

    public (uint X, uint Y, uint Z) ComputeThreadSize { get; protected set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetConstantBufferInfo(ReadOnlySpan<char> name, out ConstantBufferInfo info) {
        foreach (ref readonly var it in ConstantBufferInfos.AsSpan()) {
            if (name.SequenceEqual(it.Name)) {
                info = it;
                return true;
            }
        }

        info = default;
        return false;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetConstantBufferInfo(ResourceBindLocation location, out ConstantBufferInfo info) {
        foreach (ref readonly var it in ConstantBufferInfos.AsSpan()) {
            if (it.Location == location) {
                info = it;
                return true;
            }
        }

        info = default;
        return false;
    }
    public bool HasConstantBuffer(ReadOnlySpan<char> name) => TryGetConstantBufferInfo(name, out _);
    public bool HasConstantBuffer(ResourceBindLocation location) => TryGetConstantBufferInfo(location, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetReadonlyResourceInfo(ReadOnlySpan<char> name, out ReadonlyResourceInfo info) {
        foreach (ref readonly var it in ReadonlyResourceInfos.AsSpan()) {
            if (name.SequenceEqual(it.Name)) {
                info = it;
                return true;
            }
        }

        info = default;
        return false;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetReadonlyResourceInfo(ResourceBindLocation location, out ReadonlyResourceInfo info) {
        foreach (ref readonly var it in ReadonlyResourceInfos.AsSpan()) {
            if (it.Location == location) {
                info = it;
                return true;
            }
        }

        info = default;
        return false;
    }

    public bool HasReadonlyResource(ReadOnlySpan<char> name) => TryGetReadonlyResourceInfo(name, out _);
    public bool HasReadonlyResource(ResourceBindLocation location) => TryGetReadonlyResourceInfo(location, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetReadWriteResourceInfo(ReadOnlySpan<char> name, out ReadWriteResourceInfo info) {
        foreach (ref readonly var it in ReadWriteResourceInfos.AsSpan()) {
            if (name.SequenceEqual(it.Name)) {
                info = it;
                return true;
            }
        }

        info = default;
        return false;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetReadWriteResourceInfo(ResourceBindLocation location, out ReadWriteResourceInfo info) {
        foreach (ref readonly var it in ReadWriteResourceInfos.AsSpan()) {
            if (it.Location == location) {
                info = it;
                return true;
            }
        }

        info = default;
        return false;
    }

    public bool HasReadWriteResource(ReadOnlySpan<char> name) => TryGetReadWriteResourceInfo(name, out _);
    public bool HasReadWriteResource(ResourceBindLocation location) => TryGetReadWriteResourceInfo(location, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetSamplerInfo(ReadOnlySpan<char> name, out SamplerInfo info) {
        foreach (ref readonly var it in SamplerInfos.AsSpan()) {
            if (name.SequenceEqual(it.Name)) {
                info = it;
                return true;
            }
        }

        info = default;
        return false;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetSamplerInfo(ResourceBindLocation location, out SamplerInfo info) {
        foreach (ref readonly var it in SamplerInfos.AsSpan()) {
            if (it.Location == location) {
                info = it;
                return true;
            }
        }

        info = default;
        return false;
    }

    public bool HasSampler(ReadOnlySpan<char> name) => TryGetSamplerInfo(name, out _);
    public bool HasSampler(ResourceBindLocation location) => TryGetSamplerInfo(location, out _);
}