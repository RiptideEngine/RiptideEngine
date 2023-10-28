namespace RiptideEngine.Core;

public interface IResourceAsset : IInstantiatable {
    /// <summary>
    /// Determine whether the current instance is an asset.
    /// </summary>
    bool IsResourceAsset { get; }
}