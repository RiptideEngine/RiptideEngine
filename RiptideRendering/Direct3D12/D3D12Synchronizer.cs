namespace RiptideRendering.Direct3D12;

internal sealed class D3D12Synchronizer(D3D12RenderingContext context) : Synchronizer {
    public override void WaitCpu(ulong fenceValue) {
        context.Queues.GraphicsQueue.WaitForFence(fenceValue);
    }
}