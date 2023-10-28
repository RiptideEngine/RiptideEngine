namespace RiptideRendering;

unsafe partial class CommandList {
    public abstract void SetIndexBuffer(NativeBufferHandle buffer, IndexFormat format, uint offset);

    public abstract void SetPipelineState(PipelineState pipelineState);

    public abstract void SetGraphicsBindingSchematic(GraphicalShader shader);

    public abstract void SetGraphicsDynamicConstantBuffer(ReadOnlySpan<char> name, ReadOnlySpan<byte> data);
    public abstract void SetGraphicsReadonlyBuffer(ReadOnlySpan<char> name, NativeBufferHandle buffer, uint structuredSize, GraphicsFormat typedBufferFormat);
    public abstract void SetGraphicsReadonlyTexture(ReadOnlySpan<char> name, TextureViewHandle viewHandle);

    //public abstract void SetComputeBindingSchematic(ComputeShader shader);
    //public abstract void SetComputeDynamicConstantBuffer(ReadOnlySpan<char> name, ReadOnlySpan<byte> data);
    //public abstract void SetComputeReadWriteBuffer(ReadOnlySpan<char> name, NativeBufferHandle buffer, uint offset);
}

public static unsafe class CommandListExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetGraphicsDynamicConstantBuffer<T>(this CommandList list, ReadOnlySpan<char> name, in T data) where T : unmanaged {
        fixed (T* pData = &data) {
            list.SetGraphicsDynamicConstantBuffer(name, new ReadOnlySpan<byte>(pData, sizeof(T)));
        }
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static void SetComputeDynamicConstantBuffer<T>(this CommandList list, ReadOnlySpan<char> name, in T data) where T : unmanaged {
    //    fixed (T* pData = &data) {
    //        list.SetComputeDynamicConstantBuffer(name, new ReadOnlySpan<byte>(pData, sizeof(T)));
    //    }
    //}
}