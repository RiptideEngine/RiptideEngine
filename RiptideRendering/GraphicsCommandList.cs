namespace RiptideRendering;

public abstract class GraphicsCommandList : CommandList {
    public abstract void TranslateResourceState(GpuResource resource, ResourceTranslateStates destinationStates);
    public abstract void ClearRenderTarget(RenderTargetView view, Color color);
    public abstract void ClearRenderTarget(RenderTargetView view, Color color, ReadOnlySpan<Bound2DInt> areas);

    public abstract void SetRenderTarget(RenderTargetView view, DepthStencilView? depthView);

    public abstract void SetViewport(Viewport viewport);
    public abstract void SetScissorRect(Bound2DInt scissor);
    
    public abstract void SetPrimitiveTopology(RenderingPrimitiveTopology topology);
    public abstract void SetIndexBuffer(GpuBuffer? buffer, IndexFormat format, uint offset);
    public abstract void SetResourceSignature(ResourceSignature signature);
    public abstract void SetPipelineState(PipelineState pipelineState);

    public abstract void SetGraphicsShaderResourceView(uint parameterIndex, uint tableOffset, ShaderResourceView view);
    public abstract void SetGraphicsConstants(uint parameterIndex, ReadOnlySpan<uint> constants, uint offset);
    
    public abstract void Draw(uint vertexCount, uint instanceCount, uint startVertex, uint startInstance);
    public abstract void DrawIndexed(uint indexCount, uint instanceCount, uint startIndex, uint startInstance);
}