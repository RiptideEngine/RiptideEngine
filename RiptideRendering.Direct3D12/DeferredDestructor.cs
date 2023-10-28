namespace RiptideRendering.Direct3D12;

internal unsafe sealed class DeferredDestructor : IDisposable {
    public readonly struct Candidate(ulong fence, ID3D12Resource* pResource) {
        public readonly ulong Fence = fence;
        public readonly ID3D12Resource* Resource = pResource;
    }

    private readonly Queue<Candidate> _candidates;
    private readonly object _lock;

    public DeferredDestructor() {
        _candidates = new(16);
        _lock = new();
    }

    public void QueueResource(ulong fenceValue, ID3D12Resource* pResource) {
        lock (_lock) {
            _candidates.Enqueue(new(fenceValue, pResource));
        }
    }

    public void ReleaseResources(ulong completedFenceValue) {
        lock (_lock) {
            while (_candidates.TryPeek(out var candidate)) {
                if (candidate.Fence > completedFenceValue) break;

                candidate.Resource->Release();
                _candidates.Dequeue();
            }
        }
    }

    public void Dispose() {
        lock (_lock) {
            while (_candidates.TryDequeue(out var dequeue)) {
                dequeue.Resource->Release();
            }
        }
    }
}