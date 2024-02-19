using RiptideMathematics;

namespace Riptide.LowLevel.TextEngine;

public interface IGlyphTexture {
    Bound2D Boundary { get; }
    uint Index { get; }
}