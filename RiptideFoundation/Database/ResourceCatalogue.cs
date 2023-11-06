namespace RiptideFoundation.Database;

internal sealed class ResourceCatalogue {
    private readonly Dictionary<object, ImportingLocation> _resourceToLocation;
    private readonly Dictionary<ImportingLocation, object> _locationToResource;

    public ResourceCatalogue() {
        _resourceToLocation = [];
        _locationToResource = [];
    }

    public bool Contains(ImportingLocation location) => _locationToResource.ContainsKey(location);
    public bool Contains(object resource) => _resourceToLocation.ContainsKey(resource);

    public bool TryMapLocationToResource(ImportingLocation location, [NotNullWhen(true)] out object? resource) => _locationToResource.TryGetValue(location, out resource);
    public bool TryMapResourceToLocation(object resource, out ImportingLocation location) => _resourceToLocation.TryGetValue(resource, out location);

    public void Add(ImportingLocation location, object resource) {
        _resourceToLocation.Add(resource, location);
        _locationToResource.Add(location, resource);
    }

    public IEnumerable<object> EnumerateResources() => _resourceToLocation.Keys;
}