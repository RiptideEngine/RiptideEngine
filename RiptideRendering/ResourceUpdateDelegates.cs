namespace RiptideRendering;

public delegate void BufferWriter<in T>(Span<byte> destination, T arg);
public delegate void TextureWriter<in T>(Span<byte> destination, uint row, T arg);