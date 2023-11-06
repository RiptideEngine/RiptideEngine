namespace RiptideRendering;

public struct ConstantBufferBinding {
    public uint TableIndex;
    public uint ResourceOffset;

    public NativeResourceHandle Resource;
    public uint Offset;
}

public struct ReadonlyResourceBinding(uint tableIndex, uint resourceOffset, NativeResourceView view) {
    public uint TableIndex = tableIndex;
    public uint ResourceOffset = resourceOffset;

    public NativeResourceView View = view;
}

public struct UnorderedAccessResourceBinding {
    public uint TableIndex;
    public uint ResourceOffset;

    public NativeResourceView View;
}

unsafe partial class CommandList {
    public abstract void SetIndexBuffer(NativeResourceHandle buffer, IndexFormat format, uint offset);
    public abstract void SetGraphicsPipeline(PipelineState shader, ResourceSignature pipelineResource, ReadOnlySpan<ConstantBufferBinding> constantBuffers, ReadOnlySpan<ReadonlyResourceBinding> readonlyResources, ReadOnlySpan<UnorderedAccessResourceBinding> unorderedAccesses);
}