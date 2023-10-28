namespace RiptideFoundation;

public abstract class RenderingPipeline {
    public abstract void ExecuteRenderingOperation(in RenderingOperationData info);

    public abstract void BindMesh(CommandList cmdList, Mesh mesh);
}
