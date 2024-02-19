namespace RiptideFoundation.Rendering;

public sealed partial class MaterialPipeline : RenderingObject {
    private GraphicalShader _shader;
    private ResourceSignature _signature;
    private PipelineState _state;
    private CompactedShaderReflection _reflection;

    public GraphicalShader Shader => _shader;
    public ResourceSignature ResourceSignature => _signature;
    public PipelineState PipelineState => _state;
    public CompactedShaderReflection Reflection => _reflection;

    public override string? Name {
        get => base.Name;
        set {
            if (GetReferenceCount() == 0) return;

            base.Name = value;
            _state.Name = $"{value}.PipelineState";
        }
    }

    public MaterialPipeline(GraphicalShader shader, CompactedShaderReflection reflection, in ResourceSignatureDescription signatureDesc, in PipelineStateDescription pipelineDesc) {
        ArgumentNullException.ThrowIfNull(shader, nameof(shader));
        ArgumentNullException.ThrowIfNull(reflection, nameof(reflection));
        
        _shader = shader;
        _signature = Graphics.ResourceSignatureStorage.Get(signatureDesc);
        _state = Graphics.RenderingContext.Factory.CreatePipelineState(_shader, _signature, pipelineDesc);
        _reflection = reflection;

        _shader.IncrementReference();
        _signature.IncrementReference();
        
        _refcount = 1;
    }

    public void BindGraphics(CommandList cmdList) {
        cmdList.SetGraphicsResourceSignature(_signature);
        cmdList.SetPipelineState(_state);
    }
    
    protected override void Dispose() {
        _shader.DecrementReference(); _shader = null!;
        _state.DecrementReference(); _state = null!;
        _signature.DecrementReference(); _signature = null!;
        _reflection = null!;
    }
}