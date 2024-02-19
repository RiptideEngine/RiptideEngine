namespace RiptideEngine.Audio;

public abstract class AudioObject : ReferenceCounted {
    public virtual string? Name { get; protected set; }
}