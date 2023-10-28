namespace RiptideRendering.Shadering;

public static unsafe class DxcExtension {
    public static ReadOnlySpan<byte> AsSpan(this IDxcBlob blob) => new(blob.GetBufferPointer(), (int)blob.GetBufferSize());
    public static ReadOnlySpan<byte> AsSpan(this ComPtr<IDxcBlob> blob) => new(blob.Handle->GetBufferPointer(), (int)blob.Handle->GetBufferSize());
}