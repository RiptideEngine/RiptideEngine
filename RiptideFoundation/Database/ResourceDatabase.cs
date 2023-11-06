namespace RiptideFoundation.Database;

internal sealed partial class ResourceDatabase : IResourceDatabase {
    private bool _disposed;
    private readonly Dictionary<int, IdentifierCatalogue> _catalogues = [];
    private readonly Dictionary<int, ProtocolProvider> _protocolProviders = [];
    private readonly List<ResourceImporter> _importers = [];
    private readonly List<ResourceDisposer> _disposers = [];

    public ResourceDatabase() { }

    private void Dispose(bool disposing) {
        if (!_disposed) {
            if (disposing) {
                foreach ((_, var catalogue) in _resourceCache) {
                    foreach (var resource in catalogue.EnumerateResources()) {
                        foreach (var disposer in _disposers) {
                            if (disposer.TryDispose(resource)) break;
                        }
                    }
                }
            }

            _disposed = true;
        }
    }

    ~ResourceDatabase() {
        Dispose(disposing: false);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private readonly record struct ResourceCache(object Asset, Type Type);
}