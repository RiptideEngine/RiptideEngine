namespace RiptideRendering.Direct3D12;

internal static unsafe class D3D12Extensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Offset(ref this CpuDescriptorHandle handle, nuint byteOffset) {
        handle.Ptr += byteOffset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Offset(ref this CpuDescriptorHandle handle, uint descriptorOffset, uint incrementSize) {
        handle.Ptr += descriptorOffset * incrementSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Offset(ref this GpuDescriptorHandle handle, ulong byteOffset) {
        handle.Ptr += byteOffset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Offset(ref this GpuDescriptorHandle handle, uint descriptorOffset, uint incrementSize) {
        handle.Ptr += descriptorOffset * incrementSize;
    }
}