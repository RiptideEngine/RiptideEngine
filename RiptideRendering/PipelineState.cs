namespace RiptideRendering;

public enum PipelineStateType {
    Graphical,
    Compute,
}

public enum PipelinePrimitiveTopology {
    Undefined,

    Point,
    Line,
    Triangle,
    Patch,
}

public abstract class PipelineState : RenderingObject {
    public PipelineStateType Type { get; protected set; }
    public Shader Shader { get; protected set; } = null!;
}