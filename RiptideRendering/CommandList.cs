namespace RiptideRendering;

public abstract partial class CommandList : RenderingObject {
    public delegate void ResourceWriter<in T>(Span<byte> data, uint row, T state);

    public bool IsClosed { get; protected set; }

    public abstract void TranslateState(GpuResource resource, ResourceTranslateStates newStates);

    public abstract void ClearRenderTarget(RenderTargetView view, Color color, ReadOnlySpan<Bound2DInt> clearAreas);
    public abstract void ClearDepthTexture(DepthStencilView view, DepthClearFlags flags, float depth, byte stencil, ReadOnlySpan<Bound2DInt> clearAreas);

    public abstract void UpdateBufferRegion(GpuBuffer resource, uint offset, ReadOnlySpan<byte> source);

    public abstract void UpdateResource(GpuResource resource, ReadOnlySpan<byte> data);
    public abstract void UpdateResource<T>(GpuResource resource, ResourceWriter<T> writer, T state);

    public abstract void CopyResource(GpuResource source, GpuResource destination);
    public abstract void CopyBufferRegion(GpuBuffer source, ulong sourceOffset, GpuBuffer destination, ulong destinationOffset, ulong numBytes);
    public abstract void CopyTextureRegion(GpuTexture source, Bound3DUInt sourceBox, GpuTexture destination, uint destinationX, uint destinationY, uint destinationZ);

    public abstract void SetStencilRef(uint stencilRef);
    public abstract void SetViewport(Rectangle2D area);
    public abstract void SetScissorRect(Bound2DInt area);

    public abstract void SetRenderTarget(RenderTargetView renderTarget, DepthStencilView? depthView);
    
    public abstract void SetIndexBuffer(GpuBuffer? buffer, IndexFormat format, uint offset);

    public abstract void SetPrimitiveTopology(RenderingPrimitiveTopology topology);

    public abstract void SetPipelineState(PipelineState pipelineState);
    public abstract void SetGraphicsResourceSignature(ResourceSignature signature);

    public abstract void SetGraphicsConstants(uint parameterIndex, ReadOnlySpan<uint> constants, uint offset);
    public abstract void SetGraphicsConstantBuffer(uint tableIndex, uint tableOffset, GpuBuffer resource, uint offset);
    public abstract void SetGraphicsShaderResourceView(uint tableIndex, uint tableOffset, ShaderResourceView? view);

    public abstract void Draw(uint vertexCount, uint instanceCount, uint startVertexLoc, uint startInstanceLoc);
    public abstract void DrawIndexed(uint indexCount, uint instanceCount, uint startIndexLoc, uint startInstanceLoc);

    public abstract void Close();

    [StackTraceHidden]
    protected void EnsureNotClosed() {
        if (IsClosed) throw new InvalidOperationException(ExceptionMessages.CommandListClosed);
    }
}