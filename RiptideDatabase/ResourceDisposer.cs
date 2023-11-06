namespace RiptideDatabase;

public abstract class ResourceDisposer {
    public abstract bool TryDispose(object resource);
}