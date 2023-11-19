namespace Riptide.LowLevel.TextEngine.FreeType;

public readonly unsafe struct FT_Generic {
    public readonly void* Data;
    public readonly delegate* unmanaged<void*, void> Finalizer;
}