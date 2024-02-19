using RiptideFoundation.Rendering;

namespace RiptideFoundation;

public enum RendererFlags {
    None = 0,

    FrustumCullable = 1 << 0,
}

public abstract class Renderer : Component {
    public Mesh? Mesh { get; set; }
    [JsonIgnore] public PipelineState? PipelineState { get; set; }

    public RendererFlags Flags { get; set; }

    public abstract void Render(CommandList commandList);
}