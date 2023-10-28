namespace RiptideRendering;

/// <summary>
/// Encapsulate as a list of graphics command that used for GPU rendering.
/// </summary>
public abstract partial class CommandList : RenderingObject {
    public delegate void BufferWriter<T>(Span<byte> data, T state);

    public bool IsClosed { get; protected set; }

    // General
    public abstract void TranslateResourceStates(ReadOnlySpan<ResourceTransitionDescriptor> descs);

    public abstract void ClearRenderTarget(RenderTargetHandle handle, Color color);
    public abstract void ClearRenderTarget(RenderTargetHandle handle, Color color, ReadOnlySpan<Bound2D<int>> clearAreas);

    public abstract void ClearDepthTexture(DepthStencilHandle handle, DepthTextureClearFlags flags, float depth, byte stencil);
    public abstract void ClearDepthTexture(DepthStencilHandle handle, DepthTextureClearFlags flags, float depth, byte stencil, ReadOnlySpan<Bound2D<int>> clearAreas);

    public abstract void SetStencilRef(uint stencilRef);

    // Resource Updating
    public abstract void UpdateBuffer(NativeBufferHandle resource, uint offset, ReadOnlySpan<byte> data);
    public abstract void UpdateBuffer<T>(NativeBufferHandle resource, uint offset, uint length, BufferWriter<T> writer, T state);

    public abstract void UpdateTexture(NativeTextureHandle resource, ReadOnlySpan<byte> data);

    // Resource copying
    public abstract void CopyBuffer(NativeBufferHandle source, NativeBufferHandle destination);
    public abstract void CopyTexture(NativeTextureHandle source, NativeTextureHandle destination);
    public abstract void CopyBufferRegion(NativeBufferHandle source, ulong sourceOffset, NativeBufferHandle destination, ulong destinationOffset, ulong copyLength);
    public abstract void CopyTextureRegion(NativeTextureHandle source, Bound3D<uint> sourceBox, NativeTextureHandle destination, uint destinationX, uint destinationY, uint destinationZ);
    public abstract void ReadTexture(NativeTextureHandle source, Bound3D<uint> sourceBox, NativeReadbackBufferHandle destination);

    // Pipeline configurations
    public abstract void SetViewport(Rectangle<float> area);
    public abstract void SetScissorRect(Bound2D<int> area);

    public abstract void SetRenderTarget(RenderTargetHandle renderTarget);
    public abstract void SetRenderTarget(RenderTargetHandle renderTarget, DepthStencilHandle? depthStencil);

    // Drawings
    public abstract void Draw(uint vertexCount, uint instanceCount, uint startVertexLoc, uint startInstanceLoc);
    public abstract void DrawIndexed(uint indexCount, uint instanceCount, uint startIndexLoc, uint startInstanceLoc);

    // Dispatch
    public abstract void Dispatch(uint threadGroupX, uint threadGroupY, uint threadGroupZ);

    // Others
    public abstract void Close();

    [StackTraceHidden]
    protected void EnsureNotClosed() {
        if (IsClosed) throw new InvalidOperationException(ExceptionMessages.CommandListClosed);
    }
}