using System.Numerics;

namespace Riptide.LowLevel.TextEngine;

public interface IGlyphMetrics {
    Vector2 Size { get; }
    Vector2 Bearing { get; }
}