using RiptideRendering;

namespace Riptide.UserInterface;

internal struct RenderingCommand {
    /// <summary>
    /// Scissor rect that can be used to truncate pixel rendering on GPU and cancel widget rendering on CPU.
    /// </summary>
    public Bound2DInt ScissorRect;

    public ResourceSignature OverrideSignature;

    public PipelineState PipelineState;
    
    /// <summary>
    /// Offset in CPU's side vertex list.
    /// </summary>
    public uint VertexOffset;
    
    /// <summary>
    /// Offset in CPU's side index list.
    /// </summary>
    public uint IndexOffset;
    
    /// <summary>
    /// Amount of index to draw.
    /// </summary>
    public uint IndexCount;
}