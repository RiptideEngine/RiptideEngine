namespace RiptideEngine.Core.Allocation;

public abstract class CollectionPool<TCollection> where TCollection : ICollection {
    public abstract TCollection Get();
    public abstract void Return(TCollection collection);
}

public abstract class CollectionPool<TCollection, TItem> where TCollection : ICollection<TItem> {
    public abstract TCollection Get();
    public abstract void Return(TCollection collection);
}