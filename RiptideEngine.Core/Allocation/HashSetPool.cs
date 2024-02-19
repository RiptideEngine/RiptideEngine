namespace RiptideEngine.Core.Allocation;

public abstract class HashSetPool<T> : CollectionPool<HashSet<T>, T> {
    private static HashSetPool<T> _shared = null!;

    public static HashSetPool<T> Shared {
        get {
            _shared ??= new ThreadSafeHashSetPool<T>();
            return _shared;
        }

        set { _shared = value; }
    }
}

internal sealed class ThreadSafeHashSetPool<T> : HashSetPool<T> {
    private readonly Queue<WeakReference> _sets;
    private readonly object _lock;

    public ThreadSafeHashSetPool() {
        _sets = new();
        _lock = new object();
    }

    public override HashSet<T> Get() {
        lock (_lock) {
            while (_sets.TryDequeue(out var dequeued)) {
                if (!dequeued.IsAlive) continue;

                var output = Unsafe.As<HashSet<T>>(dequeued.Target!);
                dequeued.Target = null;

                return output;
            }

            return new();
        }
    }

    public override void Return(HashSet<T> set) {
        lock (_lock) {
            set.Clear();
            _sets.Enqueue(new WeakReference(set));
        }
    }
}