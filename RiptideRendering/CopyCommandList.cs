namespace RiptideRendering;

public abstract class CopyCommandList : CommandList {
    public abstract void TranslateResourceState(GpuResource resource, ResourceTranslateStates destinationStates);
    // public abstract void CopyResource(GpuResource source, GpuResource dest);
    // public abstract void CopyBufferRegion(GpuBuffer source, ulong sourceOffset, GpuBuffer dest, ulong destOffset, ulong copyAmount);
    // public abstract void CopyTextureRegion

    public abstract void UpdateBuffer(GpuBuffer dest, ReadOnlySpan<byte> data);
    public abstract void UpdateTexture(GpuTexture dest, uint subresource, ReadOnlySpan<byte> data);
}