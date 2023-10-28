namespace RiptideFoundation.Database;

internal sealed partial class ResourceDatabase : IResourceDatabase {
    private bool _disposed;
    private readonly Dictionary<int, IdentifierCatalogue> _catalogues = [];
    private readonly Dictionary<int, ProtocolProvider> _protocolProviders = [];
    private readonly List<ResourceImporter> _importers = [];

    public ResourceDatabase() { }

    private void Dispose(bool disposing) {
        if (!_disposed) {
            if (disposing) {
                // TODO: dispose managed state (managed objects)
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