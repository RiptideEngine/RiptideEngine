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