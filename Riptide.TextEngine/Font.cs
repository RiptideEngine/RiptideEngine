using RiptideEngine.Core;

namespace Riptide.LowLevel.TextEngine;

public abstract class Font : RiptideObject {
    public uint Size { get; init; }
    
    public abstract float Ascender { get; }
    public abstract float Descender { get; }
    
    public float LineGap => Ascender - Descender;

    public override string? Name { get; set; }
}