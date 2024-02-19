namespace RiptideEngine.Core.Allocation;

public abstract class ListPool<T> : CollectionPool<List<T>, T> {
    private static ListPool<T> _shared = null!;

    public static ListPool<T> Shared {
        get {
            _shared ??= new ThreadSafeListPool<T>();
            return _shared;
        }

        set { _shared = value; }
    }
}

internal sealed class ThreadSafeListPool<T> : ListPool<T> {
    private readonly Queue<WeakReference> _lists;
    private readonly object _lock;

    public ThreadSafeListPool() {
        _lists = new();
        _lock = new object();
    }

    public override List<T> Get() {
        lock (_lock) {
            while (_lists.TryDequeue(out var dequeued)) {
                if (!dequeued.IsAlive) continue;

                var output = Unsafe.As<List<T>>(dequeued.Target!);
                dequeued.Target = null;

                return output;
            }

            return new();
        }
    }

    public override void Return(List<T> list) {
        lock (_lock) {
            list.Clear();
            _lists.Enqueue(new WeakReference(list));
        }
    }
}
