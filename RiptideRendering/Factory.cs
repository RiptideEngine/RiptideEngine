namespace RiptideRendering;

public abstract class Factory {
    public ResourceSignature CreateResourceSignature(ResourceSignatureDescription description) {
        for (int p = 0; p < description.Parameters.Length; p++) {
            ref readonly var parameter = ref description.Parameters[p];

            switch (parameter.Type) {
                case ResourceParameterType.Constants:
                    ref readonly var constants = ref parameter.Constants;

                    if (constants.NumConstants == 0) throw new ArgumentException($"Cannot create {nameof(ResourceSignature)} because parameter {p} is type Constants and contains 0 constant.");
                    break;

                case ResourceParameterType.Descriptors:
                    ref readonly var descs = ref parameter.Descriptors;

                    if (descs.NumDescriptors == 0) throw new ArgumentException($"Cannot create {nameof(ResourceSignature)} because parameter {p} is type Descriptors and has 0 descriptor.");
                    if (!descs.Type.IsDefined())throw new ArgumentException($"Cannot create {nameof(ResourceSignature)} because parameter {p} is type Descriptors and has invalid type.");
                    break;
            }
        }

        return CreateResourceSignatureImpl(description);
    }
    protected abstract ResourceSignature CreateResourceSignatureImpl(ResourceSignatureDescription description);

    public abstract GraphicalShader CreateGraphicalShader(ReadOnlySpan<byte> vsBytecode, ReadOnlySpan<byte> psBytecode);
    public abstract GraphicalShader CreateGraphicalShader(ReadOnlySpan<byte> vsBytecode, ReadOnlySpan<byte> hsBytecode, ReadOnlySpan<byte> dsBytecode, ReadOnlySpan<byte> psBytecode);

    public abstract ComputeShader CreateComputeShader(ReadOnlySpan<byte> bytecode);
    
    public PipelineState CreatePipelineState(GraphicalShader shader, ResourceSignature signature, in PipelineStateDescription description) {
        ArgumentNullException.ThrowIfNull(shader, nameof(shader));
        ArgumentNullException.ThrowIfNull(signature, nameof(signature));

        return CreatePipelineStateImpl(shader, signature, in description);
    }
    protected abstract PipelineState CreatePipelineStateImpl(GraphicalShader shader, ResourceSignature signature, in PipelineStateDescription description);

    public PipelineState CreatePipelineState(ComputeShader shader, ResourceSignature signature) {
        ArgumentNullException.ThrowIfNull(shader, nameof(shader));
        ArgumentNullException.ThrowIfNull(signature, nameof(signature));

        return CreateComputeShaderImpl(shader, signature);
    }
    protected abstract PipelineState CreateComputeShaderImpl(ComputeShader shader, ResourceSignature signature);
    
    public abstract CommandList CreateCommandList();

    public GpuBuffer CreateBuffer(in BufferDescription desc) {
        if (!desc.Type.IsDefined()) throw new ArgumentException("Unknown buffer type.");
        
        return CreateBufferImpl(desc);
    }
    protected abstract GpuBuffer CreateBufferImpl(in BufferDescription desc);

    public GpuTexture CreateTexture(in TextureDescription desc) {
        return CreateTextureImpl(desc);
    }
    protected abstract GpuTexture CreateTextureImpl(in TextureDescription desc);

    public ShaderResourceView CreateShaderResourceView(GpuResource resource, in ShaderResourceViewDescription desc) {
        ArgumentNullException.ThrowIfNull(resource, nameof(resource));
        
        if (!desc.Dimension.IsDefined()) throw new ArgumentException($"Failed to create {nameof(ShaderResourceView)} with undefined dimension.", nameof(desc));
        if (!desc.Format.IsDefined()) throw new ArgumentException($"Failed to create {nameof(ShaderResourceView)} with undefined format type.", nameof(desc));

        return CreateShaderResourceViewImpl(resource, desc);
    }
    protected abstract ShaderResourceView CreateShaderResourceViewImpl(GpuResource resource, in ShaderResourceViewDescription desc);

    public UnorderedAccessView CreateUnorderedAccessView(GpuResource resource, in UnorderedAccessViewDescription desc) {
        ArgumentNullException.ThrowIfNull(resource, nameof(resource));

        if (!desc.Dimension.IsDefined()) throw new ArgumentException($"Failed to create {nameof(UnorderedAccessView)} with undefined dimension.", nameof(desc));
        if (!desc.Format.IsDefined()) throw new ArgumentException($"Failed to create {nameof(UnorderedAccessView)} with undefined format type.", nameof(desc));
        
        return CreateUnorderedAccessViewImpl(resource, desc);
    }
    protected abstract UnorderedAccessView CreateUnorderedAccessViewImpl(GpuResource resource, in UnorderedAccessViewDescription desc);
    
    public RenderTargetView CreateRenderTargetView(GpuTexture texture, in RenderTargetViewDescription desc) {
        ArgumentNullException.ThrowIfNull(texture, nameof(texture));
        
        if (!desc.Dimension.IsDefined()) throw new ArgumentException($"Failed to create {nameof(RenderTargetView)} with undefined dimension.", nameof(desc));
        if (!desc.Format.IsDefined()) throw new ArgumentException($"Failed to create {nameof(RenderTargetView)} with undefined format type.", nameof(desc));

        return CreateRenderTargetViewImpl(texture, desc);
    }
    protected abstract RenderTargetView CreateRenderTargetViewImpl(GpuTexture texture, in RenderTargetViewDescription desc);
    
    public DepthStencilView CreateDepthStencilView(GpuTexture texture, in DepthStencilViewDescription desc) {
        ArgumentNullException.ThrowIfNull(texture, nameof(texture));
        
        if (!desc.Dimension.IsDefined()) throw new ArgumentException($"Failed to create {nameof(DepthStencilView)} with undefined dimension.", nameof(desc));
        if (!desc.Format.IsDepthFormat()) throw new ArgumentException($"{nameof(DepthStencilView)} must be created with Depth format.", nameof(desc));

        return CreateDepthStencilViewImpl(texture, desc);
    }
    protected abstract DepthStencilView CreateDepthStencilViewImpl(GpuTexture texture, in DepthStencilViewDescription desc);
}