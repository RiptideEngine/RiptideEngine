using Silk.NET.Direct3D12;

namespace RiptideRendering.Direct3D12;

internal static unsafe class CommandListOperations {
    public delegate UploadBufferProvider.AllocatedRegion UploadRegionAllocator(ulong size, ulong alignment);
    
    public static void AddResourceTransitionBarrier(GpuResource resource, uint subresource, ResourceTranslateStates destinationStates, List<ResourceBarrier> barriers) {
        IResourceStateTracking stateTracking = Unsafe.As<IResourceStateTracking>(resource);
        
        ResourceStates newStates = Converting.Convert(destinationStates);
        
        if (stateTracking.UsageState != newStates) {
            ResourceBarrier barrier = new() {
                Type = ResourceBarrierType.Transition,
                Transition = new() {
                    PResource = (ID3D12Resource*)resource.NativeResourceHandle,
                    Subresource = subresource,
                    StateBefore = stateTracking.UsageState,
                    StateAfter = newStates,
                },
                Flags = ResourceBarrierFlags.None,
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