namespace RiptideEngine.Core;

public abstract class RiptideObject : ReferenceCounted {
    public abstract string? Name { get; set; }
}