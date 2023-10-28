namespace RiptideRendering.Direct3D12;

public readonly record struct DescriptorHandle(CpuDescriptorHandle Cpu, GpuDescriptorHandle Gpu) {
    public DescriptorHandle(nuint cpu, ulong gpu) : this(Unsafe.BitCast<nuint, CpuDescriptorHandle>(cpu), Unsafe.BitCast<ulong, GpuDescriptorHandle>(gpu)) { }

    public static implicit operator CpuDescriptorHandle(DescriptorHandle handle) => handle.Cpu;
    public static implicit operator GpuDescriptorHandle(DescriptorHandle handle) => handle.Gpu;
}