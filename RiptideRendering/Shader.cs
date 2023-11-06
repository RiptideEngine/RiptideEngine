namespace RiptideRendering;

public abstract class Shader : RenderingObject {
    public ImmutableArray<ConstantBufferInfo> ConstantBuffers { get; protected set; }
    public ImmutableArray<ReadonlyResourceInfo> ReadonlyResources { get; protected set; }
    public ImmutableArray<ReadWriteResourceInfo> ReadWriteResources { get; protected set; }

    public bool TryGetConstantBufferInfo(ReadOnlySpan<char> name, out ConstantBufferInfo info) {
        foreach (ref readonly var it in ConstantBuffers.AsSpan()) {
            if (name.SequenceEqual(it.Name)) {
                info = it;
                return true;
            }
        }

        info = default;
        return false;
    }
    public bool TryGetConstantBufferInfo(uint register, uint space, out ConstantBufferInfo info) {
        foreach (ref readonly var it in ConstantBuffers.AsSpan()) {
            if (it.Register == register && it.Space == space) {
                info = it;
                return true;
            }
        }

        info = default;
        return false;
    }
    public bool HasConstantBuffer(ReadOnlySpan<char> name) => TryGetConstantBufferInfo(name, out _);
    public bool HasConstantBuffer(uint register, uint space) => TryGetConstantBufferInfo(register, space, out _);

    public bool TryGetReadonlyResourceInfo(ReadOnlySpan<char> name, out ReadonlyResourceInfo info) {
        foreach (ref readonly var it in ReadonlyResources.AsSpan()) {
            if (name.SequenceEqual(it.Name)) {
                info = it;
                return true;
            }
        }

        info = default;
        return false;
    }
    public bool TryGetReadonlyResourceInfo(uint register, uint space, out ReadonlyResourceInfo info) {
        foreach (ref readonly var it in ReadonlyResources.AsSpan()) {
            if (it.Register == register && it.Space == space) {
                info = it;
                return true;
            }
        }

        info = default;
        return false;
    }

    public bool HasReadonlyResource(ReadOnlySpan<char> name) => TryGetReadonlyResourceInfo(name, out _);
    public bool HasReadonlyResource(uint register, uint space) => TryGetReadonlyResourceInfo(register, space, out _);

    public bool TryGetReadWriteResourceInfo(ReadOnlySpan<char> name, out ReadWriteResourceInfo info) {
        foreach (ref readonly var it in ReadWriteResources.AsSpan()) {
            if (name.SequenceEqual(it.Name)) {
                info = it;
                return true;
            }
        }

        info = default;
        return false;
    }
    public bool TryGetReadWriteResourceInfo(uint register, uint space, out ReadWriteResourceInfo info) {
        foreach (ref readonly var it in ReadWriteResources.AsSpan()) {
            if (it.Register == register && it.Space == space) {
                info = it;
                return true;
            }
        }

        info = default;
        return false;
    }

    public bool HasReadWriteResource(ReadOnlySpan<char> name) => TryGetReadWriteResourceInfo(name, out _);
    public bool HasReadWriteResource(uint register, uint space) => TryGetReadWriteResourceInfo(register, space, out _);
}