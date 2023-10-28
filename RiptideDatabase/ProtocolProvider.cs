namespace RiptideDatabase;

/// <summary>
/// Interface that allows classes to provide specialized stream to load data at specific protocol and resource path.
/// </summary>
public abstract class ProtocolProvider {
    public abstract ResourceStreams ProvideStream(string path, Type resourceType);
}