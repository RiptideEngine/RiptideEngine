using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal unsafe interface IResourceStateTracking {
    public ID3D12Resource* TransitionResource { get; }
    public ResourceStates UsageState { get; set; }
    public ResourceStates TransitioningState { get; set; }
}