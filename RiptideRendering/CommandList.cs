namespace RiptideRendering;

[StructLayout(LayoutKind.Explicit)]
public struct ConstantParameterValue {
    [FieldOffset(0)] public int Int32;
    [FieldOffset(0)] public uint UInt32;
    [FieldOffset(0)] public float Float32;

    public static implicit operator ConstantParameterValue(int value) => Unsafe.BitCast<int, ConstantParameterValue>(value);
    public static implicit operator ConstantParameterValue(uint value) => Unsafe.BitCast<uint, ConstantParameterValue>(value);
    public static implicit operator ConstantParameterValue(float value) => Unsafe.BitCast<float, ConstantParameterValue>(value);
}

public abstract class CommandList : RenderingObject {
    public bool IsClosed { get; protected set; }
    
    public abstract void Reset();
    public abstract void Close();
    
    public abstract void TranslateResourceState(GpuResource resource, ResourceTranslateStates destinationState);
    public abstract void TranslateResourceState(GpuResource resource, uint subresource, ResourceTranslateStates destinationState);

    public abstract void CopyResource(GpuResource dest, GpuResource source);
    public abstract void CopyBufferRegion(GpuBuffer dest, ulong destOffset, GpuBuffer source, ulong sourceOffset, ulong numBytes);    
    
    public abstract void UpdateBuffer(GpuBuffer buffer, ReadOnlySpan<byte> data);
    public abstract void UpdateBuffer(GpuBuffer buffer, uint offset, ReadOnlySpan<byte> data);
    public abstract void UpdateBuffer<T>(GpuBuffer buffer, BufferWriter<T> writer, T state);
    public abstract void UpdateBuffer<T>(GpuBuffer buffer, uint offset, uint length, BufferWriter<T> writer, T arg);
    
    public abstract void UpdateTexture(GpuTexture texture, uint subresource, ReadOnlySpan<byte> data);
    public abstract void UpdateTexture<T>(GpuTexture texture, uint subresource, TextureWriter<T> writer, T arg);
    
    public abstract void ClearRenderTarget(RenderTargetView view, Color color);
    public abstract void ClearRenderTarget(RenderTargetView view, Color color, ReadOnlySpan<Bound2DInt> areas);

    public abstract void ClearDepthStencil(DepthStencilView view, DepthClearFlags clearFlags, float depth, byte stencil);
    public abstract void ClearDepthStencil(DepthStencilView view, DepthClearFlags clearFlags, float depth, byte stencil, ReadOnlySpan<Bound2DInt> areas);

    public abstract void SetStencilRef(byte stencil);
    public abstract void SetRenderTarget(RenderTargetView view, DepthStencilView? depthView);
    public abstract void SetRenderTargets(ReadOnlySpan<RenderTargetView> view, DepthStencilView? depthView);

    public abstract void SetViewport(Viewport viewport);
    public abstract void SetScissorRect(Bound2DInt scissor);
    
    public abstract void SetPrimitiveTopology(RenderingPrimitiveTopology topology);
    public abstract void SetIndexBuffer(GpuBuffer? buffer, IndexFormat format, uint offset);
    public abstract void SetPipelineState(PipelineState pipelineState);
    
    public abstract void SetGraphicsResourceSignature(ResourceSignature signature);

    public abstract void SetGraphicsShaderResourceView(uint parameterIndex, uint descriptorIndex, ShaderResourceView view);
    public abstract void NullifyGraphicsShaderResourceView(uint parameterIndex, uint descriptorIndex, ShaderResourceViewDimension dimension);
    
    public abstract void SetGraphicsConstantBufferView(uint parameterIndex, uint descriptorIndex, GpuBuffer? buffer, uint offset, uint size);
    public abstract void SetGraphicsConstants(uint parameterIndex, ReadOnlySpan<ConstantParameterValue> constants, uint offset);
    
    public abstract void SetComputeResourceSignature(ResourceSignature signature);
    public abstract void SetComputeShaderResourceView(uint parameterIndex, uint descriptorIndex, ShaderResourceView view);
    public abstract void NullifyComputeShaderResourceView(uint parameterIndex, uint descriptorIndex, ShaderResourceViewDimension dimension);
    
    public abstract void SetComputeUnorderedAccessView(uint parameterIndex, uint descriptorIndex, UnorderedAccessView view);
    public abstract void NullifyComputeUnorderedAccessView(uint parameterIndex, uint descriptorIndex, UnorderedAccessViewDimension dimension);
    
    public abstract void SetComputeConstants(uint parameterIndex, ReadOnlySpan<ConstantParameterValue> constants, uint offset);
    
    public abstract void Draw(uint vertexCount, uint instanceCount, uint startVertex, uint startInstance);
    public abstract void DrawIndexed(uint indexCount, uint instanceCount, uint startIndex, uint startInstance);

    public abstract void Dispatch(uint threadGroupX, uint threadGroupY, uint threadGroupZ);
}