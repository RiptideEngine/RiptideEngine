namespace RiptideRendering.Direct3D12;

internal sealed unsafe class D3D12Factory : BaseFactory {
    private readonly D3D12RenderingContext _context;

    public D3D12Factory(D3D12RenderingContext context) {
        _context = context;
    }

    public override PipelineState CreatePipelineState(GraphicalShader shader, in PipelineStateConfig config) {
        if (shader is not D3D12GraphicalShader d3d12shader) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "GraphicalShader", "Direct3D12's GraphicalShader"), nameof(shader));

        return new D3D12PipelineState(_context, d3d12shader, config);
    }

    public override PipelineState CreatePipelineState(ComputeShader shader) {
        if (shader is not D3D12ComputeShader d3d12shader) throw new ArgumentException(string.Format(ExceptionMessages.InvalidPlatformObjectArgument, "ComputeShader", "Direct3D12's ComputeShader"));

        return new D3D12PipelineState(_context, d3d12shader);
    }

    public override GraphicalShader CreateGraphicalShader(ReadOnlySpan<byte> vsBytecode, ReadOnlySpan<byte> psBytecode, ReadOnlySpan<byte> hsBytecode, ReadOnlySpan<byte> dsBytecode, ReadOnlySpan<byte> rootSignatureBytecode) {
        if (rootSignatureBytecode.Length < 4) throw new ArgumentException("Direct3D12: A valid root signature bytecode is required.");

        return new D3D12GraphicalShader(_context, rootSignatureBytecode, vsBytecode, psBytecode, hsBytecode, dsBytecode);
    }

    public override ComputeShader CreateComputeShader(ReadOnlySpan<byte> bytecode, ReadOnlySpan<byte> rootSignatureBytecode) {
        return new D3D12ComputeShader(_context, bytecode, rootSignatureBytecode);
    }

    public override CommandList CreateCommandList() {
        return _context.CommandListPool.Request();
    }

    protected override GpuBuffer CreateGpuBufferImpl(in BufferDescriptor descriptor) {
        return new D3D12GpuBuffer(_context, descriptor);
    }

    protected override RenderTarget CreateRenderTargetImpl(in RenderTargetDescriptor descriptor) {
        if (!Unsafe.As<D3D12CapabilityChecker>(_context.CapabilityChecker).CheckFormatSupport(descriptor.Format, FormatSupport1.RenderTarget)) {
            throw new PlatformNotSupportedException(string.Format(ExceptionMessages.FailedToCreateTexture_UnsupportedFormat, nameof(RenderTarget), descriptor.Format));
        }

        return new D3D12RenderTarget(_context, descriptor);
    }

    protected override DepthTexture CreateDepthTextureImpl(in DepthTextureDescriptor descriptor) {
        if (!Unsafe.As<D3D12CapabilityChecker>(_context.CapabilityChecker).CheckFormatSupport(descriptor.Format, FormatSupport1.DepthStencil)) {
            throw new PlatformNotSupportedException(string.Format(ExceptionMessages.FailedToCreateTexture_UnsupportedFormat, nameof(DepthTexture), descriptor.Format));
        }

        return new D3D12DepthTexture(_context, descriptor);
    }

    protected override Texture2D CreateTexture2DImpl(in Texture2DDescriptor descriptor) {
        if (!Unsafe.As<D3D12CapabilityChecker>(_context.CapabilityChecker).CheckFormatSupport(descriptor.Format, FormatSupport1.Texture2D)) {
            throw new PlatformNotSupportedException(string.Format(ExceptionMessages.FailedToCreateTexture_UnsupportedFormat, nameof(Texture2D), descriptor.Format));
        }

        return new D3D12Texture2D(_context, descriptor);
    }

    protected override ReadbackBuffer CreateReadbackBufferImpl(in ReadbackBufferDescriptor descriptor) {
        return new D3D12ReadbackBuffer(_context, descriptor);
    }
}