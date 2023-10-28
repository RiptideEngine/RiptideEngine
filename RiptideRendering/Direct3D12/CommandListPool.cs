namespace RiptideRendering.Direct3D12;

internal unsafe sealed class CommandListPool : IDisposable {
    private readonly List<D3D12CommandList> _pool;
    private readonly Queue<D3D12CommandList> _availables;

    private readonly object _lock;
    private D3D12RenderingContext _context;

    public CommandListPool(D3D12RenderingContext context) {
        _pool = [];
        _availables = new Queue<D3D12CommandList>();

        _lock = new();
        _context = context;
    }

    public D3D12CommandList Request() {
        lock (_lock) {
            if (_availables.TryDequeue(out var dequeue)) {
                dequeue.Reinitialize();
                return dequeue;
            }

            D3D12CommandList ctx = new(_context);
            _pool.Add(ctx);

            Console.WriteLine($"Direct3D12 - CommandListPool: New command list created.");

            return ctx;
        }
    }

    public void Return(D3D12CommandList context) {
        Debug.Assert(context != null);

        lock (_lock) {
            _availables.Enqueue(context);
        }
    }

    public void DisposeAllContexts() {
        foreach (var context in _pool) {
            context.TrueDispose();
        }
    }

    private void Dispose(bool disposing) {
        if (_pool.Count == 0) return;

        if (disposing) {
            DisposeAllContexts();
            _pool.Clear();
            _availables.Clear();
        }
    }

    ~CommandListPool() {
        Dispose(disposing: false);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}