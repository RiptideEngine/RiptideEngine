namespace RiptideRendering.Direct3D12;

internal sealed class D3D12Factory(D3D12RenderingContext context) : Factory {
    protected override ResourceSignature CreateResourceSignatureImpl(ResourceSignatureDescription description) => new D3D12ResourceSignature(context, description);
    
    public override GraphicalShader CreateGraphicalShader(ReadOnlySpan<byte> vsBytecode, ReadOnlySpan<byte> psBytecode) => new D3D12GraphicalShader(vsBytecode, psBytecode);
    public override GraphicalShader CreateGraphicalShader(ReadOnlySpan<byte> vsBytecode, ReadOnlySpan<byte> hsBytecode, ReadOnlySpan<byte> dsBytecode, ReadOnlySpan<byte> psBytecode) => new D3D12GraphicalShader(vsBytecode, hsBytecode, dsBytecode, psBytecode);

    public override ComputeShader CreateComputeShader(ReadOnlySpan<byte> bytecode) => new D3D12ComputeShader(bytecode);

    protected override PipelineState CreatePipelineStateImpl(GraphicalShader shader, ResourceSignature signature, in PipelineStateDescription description) {
        Debug.Assert(shader is D3D12GraphicalShader, "shader is D3D12GraphicalShader");
        Debug.Assert(signature is D3D12ResourceSignature, "signature is D3D12ResourceSignature");
        
        return new D3D12PipelineState(context, Unsafe.As<D3D12GraphicalShader>(shader), Unsafe.As<D3D12ResourceSignature>(signature), description);
    }
    protected override PipelineState CreateComputeShaderImpl(ComputeShader shader, ResourceSignature signature) {
        Debug.Assert(shader is D3D12ComputeShader, "shader is D3D12ComputeShader");
        Debug.Assert(signature is D3D12ResourceSignature, "signature is DD3D12ResourceSignature");

        return new D3D12PipelineState(context, Unsafe.As<D3D12ComputeShader>(shader), Unsafe.As<D3D12ResourceSignature>(signature));
    }
    
    public override CommandList CreateCommandList() => new D3D12GraphicsCommandList(context);
    
    protected override GpuBuffer CreateBufferImpl(in BufferDescription desc) => new D3D12GpuBuffer(context, desc);
    
    protected override GpuTexture CreateTextureImpl(in TextureDescription desc) => new D3D12GpuTexture(context, desc);
    
    protected override ShaderResourceView CreateShaderResourceViewImpl(GpuResource resource, in ShaderResourceViewDescription desc) {
        Debug.Assert(resource is D3D12GpuBuffer or D3D12GpuTexture, "resource is D3D12GpuBuffer or D3D12GpuTexture");

        return new D3D12ShaderResourceView(context, resource, desc);
    }

    protected override UnorderedAccessView CreateUnorderedAccessViewImpl(GpuResource resource, in UnorderedAccessViewDescription desc) {
        Debug.Assert(resource is D3D12GpuBuffer or D3D12GpuTexture, "resource is D3D12GpuBuffer or D3D12GpuTexture");

        return new D3D12UnorderedAccessView(context, resource, desc);
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