namespace RiptideRendering;

public abstract class CommandList : RenderingObject {
    public bool IsClosed { get; protected set; }
    
    public abstract void Reset();
    public abstract void Close();
}