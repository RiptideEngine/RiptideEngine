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

    public abstract void SetPrimitiveTopology(RenderingPrimitiveTopology topology);

    public abstract void SetPipelineState(PipelineState pipelineState);
    public abstract void SetGraphicsResourceSignature(ResourceSignature signature);

    public abstract void SetGraphicsConstants(uint parameterIndex, ReadOnlySpan<uint> constants, uint offset);
    public abstract void SetGraphicsConstantBuffer(uint tableIndex, uint tableOffset, NativeResourceHandle resource, uint offset);
    public abstract void SetGraphicsResourceView(uint tableIndex, uint tableOffset, NativeResourceView view);
}