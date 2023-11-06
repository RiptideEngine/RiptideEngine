namespace RiptideRendering;

public abstract class BaseFactory {
    public abstract ResourceSignature CreateResourceSignature(ReadOnlySpan<ResourceTableDescriptor> resourceTables, ReadOnlySpan<ImmutableSamplerDescriptor> immutableSamplers);
    public abstract GraphicalShader CreateGraphicalShader(ReadOnlySpan<byte> vsBytecode, ReadOnlySpan<byte> psBytecode, ReadOnlySpan<byte> hsBytecode, ReadOnlySpan<byte> dsBytecode);

    public abstract PipelineState CreatePipelineState(GraphicalShader shader, ResourceSignature resourceSignature, in PipelineStateConfig config);

    public abstract CommandList CreateCommandList();

    public GpuResource CreateResource(in ResourceDescriptor descriptor, ResourceStates initialStates = ResourceStates.Common) {
        // if (descriptor.Dimension == ResourceDimension.Unknown) throw new ArgumentOutOfRangeException(nameof(descriptor), ExceptionMessages.FailedToCreateBuffer_ZeroSize);

        return CreateResourceImpl(descriptor, initialStates);
    }
    protected abstract GpuResource CreateResourceImpl(in ResourceDescriptor descriptor, ResourceStates initialStates);

    public abstract ResourceView CreateResourceView(GpuResource texture, ResourceViewDescriptor descriptor);
    public abstract RenderTargetView CreateRenderTargetView(GpuResource texture, RenderTargetViewDescriptor descriptor);
    public abstract DepthStencilView CreateDepthStencilView(GpuResource texture, DepthStencilViewDescriptor descriptor);
}