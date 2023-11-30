namespace RiptideRendering.Direct3D12;

internal sealed class D3D12Factory(D3D12RenderingContext context) : Factory {
    protected override ResourceSignature CreateResourceSignatureImpl(ResourceSignatureDescription description) => new D3D12ResourceSignature(context, description);
    public override GraphicalShader CreateGraphicalShader(ReadOnlySpan<byte> vsBytecode, ReadOnlySpan<byte> psBytecode, ReadOnlySpan<byte> hsBytecode, ReadOnlySpan<byte> dsBytecode) => new D3D12GraphicalShader(context, vsBytecode, psBytecode, hsBytecode, dsBytecode);
    public override PipelineState CreatePipelineState(GraphicalShader shader, ResourceSignature resourceSignature, in PipelineStateDescription description) {
        Debug.Assert(shader is D3D12GraphicalShader, "shader is D3D12GraphicalShader");
        Debug.Assert(resourceSignature is D3D12ResourceSignature, "resourceSignature is D3D12ResourceSignature");

        return new D3D12PipelineState(context, Unsafe.As<D3D12GraphicalShader>(shader), Unsafe.As<D3D12ResourceSignature>(resourceSignature), description);
    }
    public override GraphicsCommandList CreateGraphicsCommandList() => new D3D12GraphicsCommandList(context);
    public override CopyCommandList CreateCopyCommandList() => new D3D12CopyCommandList(context);
    protected override GpuBuffer CreateBufferImpl(in BufferDescription desc) => new D3D12GpuBuffer(context, desc);
    protected override GpuTexture CreateTextureImpl(in TextureDescription desc) => new D3D12GpuTexture(context, desc);
    protected override ShaderResourceView CreateShaderResourceViewImpl(GpuResource resource, in ShaderResourceViewDescription desc) {
        Debug.Assert(resource is D3D12GpuBuffer or D3D12GpuTexture, "resource is D3D12GpuBuffer or D3D12GpuTexture");

        return new D3D12ShaderResourceView(context, resource, desc);
    }
    protected override RenderTargetView CreateRenderTargetViewImpl(GpuTexture texture, in RenderTargetViewDescription desc) {
        Debug.Assert(texture is D3D12GpuTexture, "texture is D3D12GpuTexture");

        return new D3D12RenderTargetView(context, Unsafe.As<D3D12GpuTexture>(texture), desc);
    }
    protected override DepthStencilView CreateDepthStencilViewImpl(GpuTexture texture, in DepthStencilViewDescription desc) {
        Debug.Assert(texture is D3D12GpuTexture, "texture is D3D12GpuTexture");

        return new D3D12DepthStencilView(context, Unsafe.As<D3D12GpuTexture>(texture), desc);
    }
}