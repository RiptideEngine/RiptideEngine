namespace RiptideRendering;

/// <summary>
/// Encapsulate as a list of graphics command that used for GPU rendering.
/// </summary>
public abstract partial class CommandList : RenderingObject {
    public delegate void ResourceWriter<T>(Span<byte> data, uint row, T state);

    public bool IsClosed { get; protected set; }

    // General
    public abstract void TranslateResourceStates(ReadOnlySpan<ResourceTransitionDescriptor> descs);

    public abstract void ClearRenderTarget(NativeRenderTargetView view, Color color);
    public abstract void ClearRenderTarget(NativeRenderTargetView view, Color color, ReadOnlySpan<Bound2DInt> clearAreas);

    public abstract void ClearDepthTexture(NativeDepthStencilView view, DepthClearFlags flags, float depth, byte stencil);
    public abstract void ClearDepthTexture(NativeDepthStencilView view, DepthClearFlags flags, float depth, byte stencil, ReadOnlySpan<Bound2DInt> clearAreas);

    // Resource updating
    public abstract void UpdateBufferRegion(NativeResourceHandle resource, uint offset, ReadOnlySpan<byte> source);

    public abstract void UpdateResource(NativeResourceHandle resource, ReadOnlySpan<byte> data);
    public abstract void UpdateResource<T>(NativeResourceHandle resource, ResourceWriter<T> writer, T state);

    // Resource copying
    public abstract void CopyBuffer(NativeResourceHandle source, NativeResourceHandle destination);
    public abstract void CopyBufferRegion(NativeResourceHandle source, ulong sourceOffset, NativeResourceHandle destination, ulong destinationOffset, ulong numBytes);
    public abstract void CopyTextureRegion(NativeResourceHandle source, Bound3DUInt sourceBox, NativeResourceHandle destination, uint destinationX, uint destinationY, uint destinationZ);

    // Pipeline configurations
    public abstract void SetStencilRef(uint stencilRef);
    public abstract void SetViewport(Rectangle2D area);
    public abstract void SetScissorRect(Bound2DInt area);

    public abstract void SetRenderTarget(NativeRenderTargetView renderTarget);
    public abstract void SetRenderTarget(NativeRenderTargetView renderTarget, NativeDepthStencilView depthStencil);

    // Drawings
    public abstract void Draw(uint vertexCount, uint instanceCount, uint startVertexLoc, uint startInstanceLoc);
    public abstract void DrawIndexed(uint indexCount, uint instanceCount, uint startIndexLoc, uint startInstanceLoc);

    // Dispatch
    // public abstract void Dispatch(uint threadGroupX, uint threadGroupY, uint threadGroupZ);

    // Others
    public abstract void Close();

    [StackTraceHidden]
    protected void EnsureNotClosed() {
        if (IsClosed) throw new InvalidOperationException(ExceptionMessages.CommandListClosed);
    }
}