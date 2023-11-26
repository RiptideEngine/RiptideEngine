namespace RiptideRendering;

public abstract class BaseFactory {
    public ResourceSignature CreateResourceSignature(ResourceSignatureDescriptor descriptor) {
        for (int p = 0; p < descriptor.Parameters.Length; p++) {
            ref readonly var parameter = ref descriptor.Parameters[p];

            switch (parameter.Type) {
                case ResourceParameterType.Constants:
                    ref readonly var constants = ref parameter.Constants;

                    if (constants.NumConstants == 0) throw new ArgumentException($"Cannot create {nameof(ResourceSignature)} because parameter {p} is type Constants and contains 0 constant.");
                    break;

                case ResourceParameterType.Table:
                    ref readonly var table = ref parameter.Table;

                    if (table.Ranges.Length == 0) throw new ArgumentException($"Cannot create {nameof(ResourceSignature)} because parameter {p} is type Table and contains 0 range.");

                    for (int r = 0; r < table.Ranges.Length; r++) {
                        ref readonly var range = ref table.Ranges[r];

                        if (range.NumResources == 0) throw new ArgumentException($"Cannot create {nameof(ResourceSignature)} because range {r} of table {p} contains 0 resource.");
                        if (!range.Type.IsDefined()) throw new ArgumentException($"Cannot create {nameof(ResourceSignature)} because range {r} of table {p} has undefined type.");
                    }

                    var firstType = table.Ranges[0].Type;
                    if (firstType == ResourceRangeType.Sampler) {
                        for (int r = 1; r < table.Ranges.Length; r++) {
                            ref readonly var range = ref table.Ranges[r];

                            if (range.Type != ResourceRangeType.Sampler) throw new ArgumentException($"Cannot create {nameof(ResourceSignature)} because table {p} mix between resource view and sampler.");
                        }
                    } else {
                        for (int r = 1; r < table.Ranges.Length; r++) {
                            ref readonly var range = ref table.Ranges[r];

                            if (range.Type == ResourceRangeType.Sampler) throw new ArgumentException($"Cannot create {nameof(ResourceSignature)} because table {p} mix between resource view and sampler.");
                        }
                    }
                    break;
            }
        }

        return CreateResourceSignatureImpl(descriptor);
    }
    protected abstract ResourceSignature CreateResourceSignatureImpl(ResourceSignatureDescriptor descriptor);

    public abstract GraphicalShader CreateGraphicalShader(ReadOnlySpan<byte> vsBytecode, ReadOnlySpan<byte> psBytecode, ReadOnlySpan<byte> hsBytecode, ReadOnlySpan<byte> dsBytecode);

    public abstract PipelineState CreatePipelineState(GraphicalShader shader, ResourceSignature resourceSignature, in PipelineStateDescription description);

    public abstract CommandList CreateCommandList();

    public GpuBuffer CreateBuffer(in BufferDescription desc) {
        return CreateBufferImpl(desc);
    }
    protected abstract GpuBuffer CreateBufferImpl(in BufferDescription desc);

    public GpuTexture CreateTexture(in TextureDescription desc) {
        return CreateTextureImpl(desc);
    }
    protected abstract GpuTexture CreateTextureImpl(in TextureDescription desc);

    public ShaderResourceView CreateShaderResourceView(GpuResource resource, in ShaderResourceViewDescription desc) {
        if (!desc.Dimension.IsDefined()) throw new ArgumentException($"Failed to create {nameof(ShaderResourceView)} with undefined dimension.", nameof(desc));
        if (!desc.Format.IsDefined()) throw new ArgumentException($"Failed to create {nameof(ShaderResourceView)} with undefined format type.", nameof(desc));

        return CreateShaderResourceViewImpl(resource, desc);
    }
    protected abstract ShaderResourceView CreateShaderResourceViewImpl(GpuResource resource, in ShaderResourceViewDescription desc);
    
    public RenderTargetView CreateRenderTargetView(GpuTexture texture, in RenderTargetViewDescription desc) {
        if (!desc.Dimension.IsDefined()) throw new ArgumentException($"Failed to create {nameof(RenderTargetView)} with undefined dimension.", nameof(desc));
        if (!desc.Format.IsDefined()) throw new ArgumentException($"Failed to create {nameof(RenderTargetView)} with undefined format type.", nameof(desc));

        return CreateRenderTargetViewImpl(texture, desc);
    }
    protected abstract RenderTargetView CreateRenderTargetViewImpl(GpuTexture texture, in RenderTargetViewDescription desc);
    
    public DepthStencilView CreateDepthStencilView(GpuTexture texture, in DepthStencilViewDescription desc) {
        if (!desc.Dimension.IsDefined()) throw new ArgumentException($"Failed to create {nameof(DepthStencilView)} with undefined dimension.", nameof(desc));
        if (!desc.Format.IsDepthFormat()) throw new ArgumentException($"{nameof(DepthStencilView)} must be created with Depth format.", nameof(desc));

        return CreateDepthStencilViewImpl(texture, desc);
    }
    protected abstract DepthStencilView CreateDepthStencilViewImpl(GpuTexture texture, in DepthStencilViewDescription desc);
}