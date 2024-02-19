namespace RiptideEngine.Core.Allocation;

public abstract class DictionaryPool<TKey, TValue> : CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>> where TKey : notnull {
    private static DictionaryPool<TKey, TValue> _shared = null!;

    public static DictionaryPool<TKey, TValue> Shared {
        get {
            _shared ??= new ThreadSafeDictionaryPool<TKey, TValue>();
            return _shared;
        }

        set { _shared = value; }
    }
}

internal sealed class ThreadSafeDictionaryPool<TKey, TValue> : DictionaryPool<TKey, TValue> where TKey : notnull {
    private readonly Queue<WeakReference> _dictionaries;
    private readonly object _lock;

    public ThreadSafeDictionaryPool() {
        _dictionaries = new();
        _lock = new object();
    }

    public override Dictionary<TKey, TValue> Get() {
        lock (_lock) {
            while (_dictionaries.TryDequeue(out var dequeued)) {
                if (!dequeued.IsAlive) continue;

                var output = Unsafe.As<Dictionary<TKey, TValue>>(dequeued.Target!);
                dequeued.Target = null;

                return output;
            }

            return new();
        }
    }

    public override void Return(Dictionary<TKey, TValue> dictionary) {
        lock (_lock) {
            dictionary.Clear();
            _dictionaries.Enqueue(new WeakReference(dictionary));
        }
    }
}
