namespace RiptideDatabase;

public readonly record struct ImportingLocation(string Protocol, Guid ResourceGuid) {
    public override string ToString() {
        return $"{Protocol}: {ResourceGuid}";
    }
}