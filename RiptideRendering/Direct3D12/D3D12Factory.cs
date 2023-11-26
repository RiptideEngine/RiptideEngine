namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12Factory : BaseFactory {
    private readonly D3D12RenderingContext _context;

    public D3D12Factory(D3D12RenderingContext context) {
        _context = context;
    }

    public override PipelineState CreatePipelineState(GraphicalShader shader, ResourceSignature pipelineResource, in PipelineStateDescription description) {
        if (shader is not D3D12GraphicalShader d3d12shader) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "GraphicalShader", "Direct3D12's GraphicalShader"), nameof(shader));
        if (pipelineResource is not D3D12ResourceSignature d3d12pr) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "PipelineResource", "Direct3D12's PipelineResource"), nameof(pipelineResource));

        return new D3D12PipelineState(_context, d3d12shader, d3d12pr, description);
    }

    protected override ResourceSignature CreateResourceSignatureImpl(ResourceSignatureDescriptor descriptor) {
        return new D3D12ResourceSignature(_context, descriptor);
    }

    public override GraphicalShader CreateGraphicalShader(ReadOnlySpan<byte> vsBytecode, ReadOnlySpan<byte> psBytecode, ReadOnlySpan<byte> hsBytecode, ReadOnlySpan<byte> dsBytecode) {
        return new D3D12GraphicalShader(_context, vsBytecode, psBytecode, hsBytecode, dsBytecode);
    }

    public override CommandList CreateCommandList() {
        return _context.CommandListPool.Request();
    }

    protected override GpuBuffer CreateBufferImpl(in BufferDescription desc) => new D3D12GpuBuffer(_context, desc);
    protected override GpuTexture CreateTextureImpl(in TextureDescription desc) => new D3D12GpuTexture(_context, desc);

    protected override ShaderResourceView CreateShaderResourceViewImpl(GpuResource resource, in ShaderResourceViewDescription desc) {
        if (resource is not D3D12GpuTexture && resource is not D3D12GpuBuffer) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "GpuResource", "Direct3D12's GpuBuffer or GpuTexture"));

        return new D3D12ShaderResourceView(_context, resource, desc);
    }

    protected override RenderTargetView CreateRenderTargetViewImpl(GpuTexture texture, in RenderTargetViewDescription desc) {
        if (texture is not D3D12GpuTexture d3d12texture) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "GpuTexture", "Direct3D12's GpuTexture"));

        return new D3D12RenderTargetView(_context, d3d12texture, desc);
    }

    protected override DepthStencilView CreateDepthStencilViewImpl(GpuTexture texture, in DepthStencilViewDescription desc) {
        if (texture is not D3D12GpuTexture d3d12texture) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "GpuTexture", "Direct3D12's GpuTexture"));

        return new D3D12DepthStencilView(_context, d3d12texture, desc);
    }
}