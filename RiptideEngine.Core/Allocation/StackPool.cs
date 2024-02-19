namespace RiptideEngine.Core.Allocation;

public abstract class StackPool<T> : CollectionPool<Stack<T>> {
    private static StackPool<T> _shared = null!;

    public static StackPool<T> Shared {
        get {
            _shared ??= new ThreadSafeStackPool<T>();
            return _shared;
        }

        set { _shared = value; }
    }
}

internal sealed class ThreadSafeStackPool<T> : StackPool<T> {
    private readonly Queue<WeakReference> _stacks;
    private readonly object _lock;

    public ThreadSafeStackPool() {
        _stacks = new();
        _lock = new object();
    }

    public override Stack<T> Get() {
        lock (_lock) {
            while (_stacks.TryDequeue(out var dequeued)) {
                if (!dequeued.IsAlive) continue;

                var output = Unsafe.As<Stack<T>>(dequeued.Target!);
                dequeued.Target = null;

                return output;
            }

            return new();
        }
    }

    public override void Return(Stack<T> stack) {
        lock (_lock) {
            stack.Clear();
            _stacks.Enqueue(new WeakReference(stack));
        }
    }
}
