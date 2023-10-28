namespace RiptideRendering.Direct3D12;

unsafe partial class D3D12CommandList {
    public override void Draw(uint vertexCount, uint instanceCount, uint startVertexLoc, uint startInstanceLoc) {
        if (_graphicalShader == null) return;
        EnsureNotClosed();

        EnsureAllGraphicsRootDescriptorsBounded();

        var reflector = Unsafe.As<D3D12ShaderReflector>(_graphicalShader.Reflector);
        ref var rsdesc = ref _graphicalShader.GetRootSignatureDesc()->Desc10;

        _descCommitter.CommitGraphics(pCommandList);

        pCommandList.DrawInstanced(vertexCount, instanceCount, startVertexLoc, startInstanceLoc);
    }

    public override void DrawIndexed(uint indexCount, uint instanceCount, uint startIndexLoc, uint startInstanceLoc) {
        if (_graphicalShader == null) return;
        EnsureNotClosed();

        EnsureAllGraphicsRootDescriptorsBounded();

        var reflector = Unsafe.As<D3D12ShaderReflector>(_graphicalShader.Reflector);
        ref var rsdesc = ref _graphicalShader.GetRootSignatureDesc()->Desc10;

        _descCommitter.CommitGraphics(pCommandList);
        pCommandList.DrawIndexedInstanced(indexCount, instanceCount, startIndexLoc, 0, startInstanceLoc);
    }

    public override void Dispatch(uint threadGroupX, uint threadGroupY, uint threadGroupZ) {
        if (_computeShader == null) return;
        EnsureNotClosed();

        EnsureAllComputeRootDescriptorBounded();

        var reflector = Unsafe.As<D3D12ShaderReflector>(_computeShader.Reflector);
        ref var rsdesc = ref _computeShader.GetRootSignatureDesc()->Desc10;

        _descCommitter.CommitCompute(pCommandList);
        pCommandList.Dispatch(threadGroupX, threadGroupY, threadGroupZ);
    }

    // TODO: ExecuteIndirect
}