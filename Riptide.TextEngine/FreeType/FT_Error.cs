namespace Riptide.LowLevel.TextEngine.FreeType;

public readonly record struct FT_Error(int Code) {
    public readonly bool IsError => Code != 0;
}