namespace RiptideRendering;

public enum PipelinePrimitiveTopology {
    Undefined,

    Point,
    Line,
    Triangle,
    Patch,
}

public abstract class PipelineState : RenderingObject;