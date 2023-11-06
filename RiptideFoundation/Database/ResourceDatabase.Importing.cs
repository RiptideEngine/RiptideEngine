using RPCSToolkit;

namespace RiptideFoundation.Database;

partial class ResourceDatabase {
    private readonly List<(Type Type, ResourceCatalogue Catalogue)> _resourceCache = [];

    public ImportingResult LoadResource(ImportingLocation location, Type resourceType, ImportingContext? context) {
        if (string.IsNullOrEmpty(location.Protocol)) return ImportingResult.FromError(ImportingError.UnknownProtocol);
        if (resourceType == null) return ImportingResult.FromResult(ImportingError.NullResourceType);

        var protocol = location.Protocol.ToLower();

        foreach ((var type, var rcatalogue) in _resourceCache) {
            if (!type.IsAssignableTo(resourceType)) continue;

            if (rcatalogue.TryMapLocationToResource(location, out object? cached)) {
                return ImportingResult.FromResult(cached);
            }
        }

        if (!_catalogues.TryGetValue(protocol.GetHashCode(), out var icatalogue)) return ImportingResult.FromError(ImportingError.MissingCatalogue);
        if (!icatalogue.TryMapGuidToPath(location.ResourceGuid, out var path)) return ImportingResult.FromError(ImportingError.UnmappedResourceGuid);

        if (!_protocolProviders.TryGetValue(string.GetHashCode(protocol), out var protocolProvider)) return ImportingResult.FromError(ImportingError.MissingProtocolProvider);

        foreach (var importer in _importers) {
            if (!importer.CanImport(location, resourceType)) continue;

            using var stream = protocolProvider.ProvideStream(path, resourceType);

            if (stream.ResourceStream == null) return ImportingResult.FromError(ImportingError.NullResourceStream);

            context ??= new ImportingContext();

            var result = importer.RawImport(stream);

            if (result.HasError) return result;
            if (result.Result == null) return ImportingResult.FromError(ImportingError.EmptyResult);

            var dto = result.Result;

            result = importer.ImportPartially(dto);

            var importObject = result.Result;

            if (result.HasError) return result;
            if (importObject == null) return ImportingResult.FromError(ImportingError.EmptyResult);

            ResourceCatalogue? cacheTable = null;
            foreach ((var type, var table) in _resourceCache) {
                if (!type.IsAssignableTo(resourceType)) continue;

                cacheTable = table;
                break;
            }

            if (cacheTable == null) {
                cacheTable = new();
                _resourceCache.Add((resourceType, cacheTable));
            }

            cacheTable.Add(new(protocol, location.ResourceGuid), result.Result!);

            context.PushDependencyScope();
            importer.GetDependencies(context, dto);

            var dependprofiles = context.PopDependencyScope();
            if (dependprofiles.Count != 0) {
                var outputs = DictionaryPool<string, object?>.Shared.Get();

                try {
                    foreach ((var key, var profile) in dependprofiles) {
                        var loadResult = LoadResource(profile.Location, profile.ResourceType, context);

                        outputs.Add(key, loadResult.Result);
                    }

                    importer.PatchDependencies(dto, importObject, outputs);
                } finally {
                    DictionaryPool<string, object?>.Shared.Return(outputs);
                }
            }

            return ImportingResult.FromResult(result.Result!);
        }

        return ImportingResult.FromError(ImportingError.MissingResourceProvider);
    }

    public bool TryGetResourceImportLocation(object resource, out ImportingLocation location) {
        if (resource == null) goto failure;

        var rtype = resource.GetType();
        foreach ((var type, var catalogue) in _resourceCache) {
            if (!rtype.IsAssignableTo(type)) continue;

            return catalogue.TryMapResourceToLocation(resource, out location);
        }

        failure:
        location = default;
        return false;
    }
}