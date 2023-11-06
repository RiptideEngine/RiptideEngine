namespace RiptideFoundation.Database;

partial class ResourceDatabase {
    public bool RegisterIdentifierCatalogue(ReadOnlySpan<char> protocol, IdentifierCatalogue catalogue) {
        Span<char> lowercaseProtocol = protocol.Length > 16 ? new char[protocol.Length] : stackalloc char[protocol.Length];
        protocol.ToLower(lowercaseProtocol, null);

        ref var refcatalogue = ref CollectionsMarshal.GetValueRefOrAddDefault(_catalogues, string.GetHashCode(lowercaseProtocol), out bool exists);
        if (exists) return false;

        refcatalogue = catalogue;
        return true;
    }
    public bool UnregisterIdentifierCatalogue(ReadOnlySpan<char> protocol, [NotNullWhen(true)] out IdentifierCatalogue? catalogue) {
        Span<char> lowercaseProtocol = protocol.Length > 16 ? new char[protocol.Length] : stackalloc char[protocol.Length];
        protocol.ToLower(lowercaseProtocol, null);

        return _catalogues.Remove(string.GetHashCode(lowercaseProtocol), out catalogue);
    }
    public bool TryGetIdentifierCatalogue(ReadOnlySpan<char> protocol, [NotNullWhen(true)] out IdentifierCatalogue? catalogue) {
        Span<char> lowercaseProtocol = protocol.Length > 16 ? new char[protocol.Length] : stackalloc char[protocol.Length];
        protocol.ToLower(lowercaseProtocol, null);

        return _catalogues.TryGetValue(string.GetHashCode(lowercaseProtocol), out catalogue);
    }

    public bool RegisterProtocolProvider(ReadOnlySpan<char> protocol, ProtocolProvider provider) {
        Span<char> lowercaseProtocol = protocol.Length > 16 ? new char[protocol.Length] : stackalloc char[protocol.Length];
        protocol.ToLower(lowercaseProtocol, null);

        ref var refprovider = ref CollectionsMarshal.GetValueRefOrAddDefault(_protocolProviders, string.GetHashCode(lowercaseProtocol), out bool exists);
        if (exists) return false;

        refprovider = provider;
        return true;
    }
    public bool UnregisterProtocolProvider(ReadOnlySpan<char> protocol, [NotNullWhen(true)] out ProtocolProvider? provider) {
        Span<char> lowercaseProtocol = protocol.Length > 16 ? new char[protocol.Length] : stackalloc char[protocol.Length];
        protocol.ToLower(lowercaseProtocol, null);

        return _protocolProviders.Remove(string.GetHashCode(lowercaseProtocol), out provider);
    }
    public bool TryGetProtocolProvider(ReadOnlySpan<char> protocol, [NotNullWhen(true)] out ProtocolProvider? provider) {
        Span<char> lowercaseProtocol = protocol.Length > 16 ? new char[protocol.Length] : stackalloc char[protocol.Length];
        protocol.ToLower(lowercaseProtocol, null);

        return _protocolProviders.TryGetValue(string.GetHashCode(lowercaseProtocol), out provider);
    }

    public void RegisterResourceImporter(ResourceImporter importer) => _importers.Add(importer);
    public bool UnregisterResourceImporter(ResourceImporter importer) => _importers.Remove(importer);

    public void RegisterResourceDisposer(ResourceDisposer disposer) => _disposers.Add(disposer);
    public bool UnregisterResourceDisposer(ResourceDisposer disposer) => _disposers.Remove(disposer);
}