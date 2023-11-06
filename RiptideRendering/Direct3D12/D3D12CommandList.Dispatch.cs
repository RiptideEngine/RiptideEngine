namespace RiptideRendering.Direct3D12;

unsafe partial class D3D12CommandList {
    public override void Draw(uint vertexCount, uint instanceCount, uint startVertexLoc, uint startInstanceLoc) {
        EnsureNotClosed();

        _descCommitter.CommitGraphics(pCommandList);

        pCommandList.DrawInstanced(vertexCount, instanceCount, startVertexLoc, startInstanceLoc);
    }

    public override void DrawIndexed(uint indexCount, uint instanceCount, uint startIndexLoc, uint startInstanceLoc) {
        EnsureNotClosed();

        _descCommitter.CommitGraphics(pCommandList);
        pCommandList.DrawIndexedInstanced(indexCount, instanceCount, startIndexLoc, 0, startInstanceLoc);
    }

    public override void Dispatch(uint threadGroupX, uint threadGroupY, uint threadGroupZ) {
        EnsureNotClosed();

        _descCommitter.CommitCompute(pCommandList);
        pCommandList.Dispatch(threadGroupX, threadGroupY, threadGroupZ);
    }

    // TODO: ExecuteIndirect
}