using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal readonly unsafe struct DeferredResourceDestructor() : IDisposable {
    private readonly Queue<Entry> _entries = [];

    public void Add(ID3D12Resource* pResource, ulong destroyFence) {
        _entries.Enqueue(new(pResource, destroyFence));
    }

    public void Destroy(ulong currentFence) {
        while (_entries.TryPeek(out var entry) && currentFence >= entry.DestroyFence) {
            entry.Resource->Release();
            _entries.Dequeue();
        }
    }

    public void Dispose() {
        Destroy(ulong.MaxValue);
        _entries.Clear();
    }

    private readonly struct Entry(ID3D12Resource* resource, ulong destroyFence) {
        public readonly ID3D12Resource* Resource = resource;
        public readonly ulong DestroyFence = destroyFence;
    }
}