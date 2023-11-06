namespace RiptideDatabase;

public interface IResourceDatabase : IRiptideService {
    ImportingResult LoadResource(ImportingLocation location, Type resourceType, ImportingContext? context);

    bool RegisterIdentifierCatalogue(ReadOnlySpan<char> protocol, IdentifierCatalogue catalogue);
    bool UnregisterIdentifierCatalogue(ReadOnlySpan<char> protocol, [NotNullWhen(true)] out IdentifierCatalogue? catalogue);
    bool TryGetIdentifierCatalogue(ReadOnlySpan<char> protocol, [NotNullWhen(true)] out IdentifierCatalogue? catalogue);

    bool RegisterProtocolProvider(ReadOnlySpan<char> protocol, ProtocolProvider provider);
    bool UnregisterProtocolProvider(ReadOnlySpan<char> protocol, [NotNullWhen(true)] out ProtocolProvider? provider);
    bool TryGetProtocolProvider(ReadOnlySpan<char> protocol, [NotNullWhen(true)] out ProtocolProvider? provider);

    void RegisterResourceImporter(ResourceImporter importer);
    bool UnregisterResourceImporter(ResourceImporter importer);

    void RegisterResourceDisposer(ResourceDisposer disposer);
    bool UnregisterResourceDisposer(ResourceDisposer disposer);
}