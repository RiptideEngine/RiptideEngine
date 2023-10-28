namespace RiptideRendering;

public abstract class BaseFactory {
    public abstract GraphicalShader CreateGraphicalShader(ReadOnlySpan<byte> vsBytecode, ReadOnlySpan<byte> psBytecode, ReadOnlySpan<byte> hsBytecode, ReadOnlySpan<byte> dsBytecode, ReadOnlySpan<byte> rootSignatureBytecode);
    public abstract ComputeShader CreateComputeShader(ReadOnlySpan<byte> bytecode, ReadOnlySpan<byte> rootSignatureBytecode);

    public abstract PipelineState CreatePipelineState(GraphicalShader shader, in PipelineStateConfig config);
    public abstract PipelineState CreatePipelineState(ComputeShader shader);

    public abstract CommandList CreateCommandList();

    public GpuBuffer CreateBuffer(in BufferDescriptor descriptor) {
        if (descriptor.Size == 0) throw new ArgumentOutOfRangeException(nameof(descriptor), ExceptionMessages.FailedToCreateBuffer_ZeroSize);

        return CreateGpuBufferImpl(descriptor);
    }
    protected abstract GpuBuffer CreateGpuBufferImpl(in BufferDescriptor descriptor);

    public RenderTarget CreateRenderTarget(in RenderTargetDescriptor descriptor) {
        if (descriptor.Width == 0 || descriptor.Height == 0) throw new ArgumentOutOfRangeException(nameof(descriptor), ExceptionMessages.FailedToCreate2DTexture_ZeroSize);
        if (!descriptor.Format.IsDefined()) throw new ArgumentException(ExceptionMessages.FailedToCreateTexture_UndefinedFormat, nameof(descriptor));

        return CreateRenderTargetImpl(descriptor);
    }
    protected abstract RenderTarget CreateRenderTargetImpl(in RenderTargetDescriptor descriptor);

    public DepthTexture CreateDepthTexture(in DepthTextureDescriptor descriptor) {
        if (descriptor.Width == 0 || descriptor.Height == 0) throw new ArgumentOutOfRangeException(nameof(descriptor), ExceptionMessages.FailedToCreate2DTexture_ZeroSize);
        if (descriptor.Format is not GraphicsFormat.D16UNorm and not GraphicsFormat.D24UNormS8UInt and not GraphicsFormat.D32Float and not GraphicsFormat.D32FloatS8UInt)
            throw new ArgumentException(ExceptionMessages.FailedToCreateDepthTex_MustBeDepthFormat, nameof(descriptor));

        return CreateDepthTextureImpl(descriptor);
    }
    protected abstract DepthTexture CreateDepthTextureImpl(in DepthTextureDescriptor descriptor);

    public Texture2D CreateTexture2D(in Texture2DDescriptor descriptor) {
        if (descriptor.Width == 0 || descriptor.Height == 0) throw new ArgumentOutOfRangeException(nameof(descriptor), ExceptionMessages.FailedToCreate2DTexture_ZeroSize);
        if (!descriptor.Format.IsDefined()) throw new ArgumentException(ExceptionMessages.FailedToCreateTexture_UndefinedFormat, nameof(descriptor));

        return CreateTexture2DImpl(descriptor);
    }
    protected abstract Texture2D CreateTexture2DImpl(in Texture2DDescriptor descriptor);

    public ReadbackBuffer CreateReadbackBuffer(in ReadbackBufferDescriptor descriptor) {
        if (descriptor.Size == 0) throw new ArgumentOutOfRangeException(nameof(descriptor), ExceptionMessages.FailedToCreateBuffer_ZeroSize);

        return CreateReadbackBufferImpl(descriptor);
    }
    protected abstract ReadbackBuffer CreateReadbackBufferImpl(in ReadbackBufferDescriptor descriptor);
}