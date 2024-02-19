namespace RiptideEngine.Core.Allocation;

public abstract class QueuePool<T> : CollectionPool<Queue<T>> {
    private static QueuePool<T> _shared = null!;

    public static QueuePool<T> Shared {
        get {
            _shared ??= new ThreadSafeQueuePool<T>();
            return _shared;
        }

        set { _shared = value; }
    }
}

internal sealed class ThreadSafeQueuePool<T> : QueuePool<T> {
    private readonly Queue<WeakReference> _queues;
    private readonly object _lock;

    public ThreadSafeQueuePool() {
        _queues = new();
        _lock = new object();
    }

    public override Queue<T> Get() {
        lock (_lock) {
            while (_queues.TryDequeue(out var dequeued)) {
                if (!dequeued.IsAlive) continue;

                var output = Unsafe.As<Queue<T>>(dequeued.Target!);
                dequeued.Target = null;

                return output;
            }

            return new();
        }
    }

    public override void Return(Queue<T> queue) {
        lock (_lock) {
            queue.Clear();
            _queues.Enqueue(new WeakReference(queue));
        }
    }
}
