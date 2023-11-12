namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12Factory : BaseFactory {
    private readonly D3D12RenderingContext _context;

    public D3D12Factory(D3D12RenderingContext context) {
        _context = context;
    }

    public override PipelineState CreatePipelineState(GraphicalShader shader, ResourceSignature pipelineResource, in PipelineStateConfig config) {
        if (shader is not D3D12GraphicalShader d3d12shader) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "GraphicalShader", "Direct3D12's GraphicalShader"), nameof(shader));
        if (pipelineResource is not D3D12ResourceSignature d3d12pr) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "PipelineResource", "Direct3D12's PipelineResource"), nameof(pipelineResource));

        return new D3D12PipelineState(_context, d3d12shader, d3d12pr, config);
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

    protected override GpuResource CreateResourceImpl(in ResourceDescriptor descriptor, ResourceStates initialStates) {
        return new D3D12GpuResource(_context, descriptor, initialStates);
    }

    public override ResourceView CreateResourceView(GpuResource resource, ResourceViewDescriptor descriptor) {
        if (resource is not D3D12GpuResource d3d12resource) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "GpuResource", "Direct3D12's GpuResource"));
        if (!descriptor.Format.IsDefined()) throw new ArgumentException("Failed to create ResourceView with undefined format type.", nameof(descriptor));
        if (!descriptor.Dimension.IsDefined()) throw new ArgumentException("Failed to create ResourceView with undefined dimension.");

        return new D3D12ResourceView(_context, d3d12resource, descriptor);
    }

    public override RenderTargetView CreateRenderTargetView(GpuResource texture, RenderTargetViewDescriptor descriptor) {
        if (texture is not D3D12GpuResource d3d12resource) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "GpuTexture", "Direct3D12's GpuTexture"));
        if (!((ID3D12Resource*)d3d12resource.NativeResource.Handle)->GetDesc().Flags.HasFlag(D3D12ResourceFlags.AllowRenderTarget)) throw new ArgumentException("Texture is not allowed to be used as render target.");

        return new D3D12RenderTargetView(_context, d3d12resource, descriptor);
    }

    public override DepthStencilView CreateDepthStencilView(GpuResource texture, DepthStencilViewDescriptor descriptor) {
        if (texture is not D3D12GpuResource d3d12resource) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "GpuTexture", "Direct3D12's GpuTexture"));
        if (!((ID3D12Resource*)d3d12resource.NativeResource.Handle)->GetDesc().Flags.HasFlag(D3D12ResourceFlags.AllowDepthStencil)) throw new ArgumentException("Texture is not allowed to be used as depth texture.");

        return new D3D12DepthStencilView(_context, d3d12resource, descriptor);
    }
}