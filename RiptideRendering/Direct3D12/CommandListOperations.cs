using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal static unsafe class CommandListOperations {
    public static void AddResourceTransitionBarrier(GpuResource resource, ResourceTranslateStates destinationStates, List<ResourceBarrier> barriers) {
        Debug.Assert(resource is IResourceStateTracking, "resource is IResourceStateTracking");

        IResourceStateTracking stateTracking = Unsafe.As<IResourceStateTracking>(resource);
        
        ResourceStates newStates = Converting.Convert(destinationStates);
        
        if (stateTracking.UsageState != newStates) {
            ResourceBarrier barrier = new() {
                Type = ResourceBarrierType.Transition,
                Transition = new() {
                    PResource = (ID3D12Resource*)resource.NativeResourceHandle,
                    Subresource = 0xFFFFFFFF,
                    StateBefore = stateTracking.UsageState,
                    StateAfter = newStates,
                },
            };

            if (newStates == stateTracking.TransitioningState) {
                barrier.Flags = ResourceBarrierFlags.EndOnly;
                stateTracking.TransitioningState = (ResourceStates)(-1);
            }

            stateTracking.UsageState = newStates;
            barriers.Add(barrier);
        } else if (newStates == ResourceStates.UnorderedAccess) {
            ResourceBarrier barrier = new() {
                Type = ResourceBarrierType.Uav,
                UAV = new() {
                    PResource = (ID3D12Resource*)resource.NativeResourceHandle,
                },
            };

            barriers.Add(barrier);
        }
    }
}