using Riptide.LowLevel.TextEngine;
using Riptide.LowLevel.TextEngine.FreeType;
using Riptide.LowLevel.TextEngine.Harfbuzz;
using RiptideFoundation.Rendering;

namespace RiptideFoundation.Text;

// TODO: Fallback character.
public sealed unsafe partial class SignedDistanceFont : Font {
    private FT_FaceRec* _ftFace;
    private hb_font_t* _hbFont;
    
    private readonly GlyphTextureTable<GlyphTexture> _glyphTextures;
    private readonly GlyphMetricTable<GlyphMetric> _glyphMetrics;
    
    public Texture2D Bitmap { get; }
    
    public override float Ascender => _ftFace->Size->Metrics.Ascender.Value >> 6;
    public override float Descender => _ftFace->Size->Metrics.Descender.Value >> 6;

    public hb_font_t* HarfbuzzFont => _hbFont;
    
    public override string? Name {
        get => base.Name;
        set {
            if (GetReferenceCount() == 0) return;
            
            base.Name = value;
            Bitmap.Name = $"{value}.Bitmap";
        }
    }

    private SignedDistanceFont(FT_FaceRec* ftFace, hb_font_t* hbfont, GlyphTextureTable<GlyphTexture> textureTable, GlyphMetricTable<GlyphMetric> metricTable, Texture2D bitmap, uint size) {
        _ftFace = ftFace;
        _hbFont = hbfont;
        
        _glyphTextures = textureTable;
        _glyphMetrics = metricTable;
        
        Bitmap = bitmap;
        Size = size;

        _refcount = 1;
    }

    public bool TryGetGlyphMetricInfo(uint glyphIndex, out GlyphMetric output) => _glyphMetrics.TryGetValue(glyphIndex, out output);
    public bool TryGetGlyphTextureInfo(uint glyphIndex, out GlyphTexture output) => _glyphTextures.TryGetValue(glyphIndex, out output);

    protected override void Dispose() {
        FontEngine.FreeFace(_ftFace);
        _ftFace = null;
        
        HarfbuzzApi.hb_font_destroy(_hbFont);
        _hbFont = null;
        
        Bitmap.DecrementReference();
        _glyphTextures.Clear();
    }

    public readonly struct GlyphTexture : IGlyphTexture {
        public Bound2D Boundary { get; }
        public uint Index { get; }
        public readonly Bound2D TextureStandardBoundary;
        
        internal GlyphTexture(Bound2D textureStandardBoundary, Bound2D textureFullBoundary, uint arrayIndex) {
            TextureStandardBoundary = textureStandardBoundary;
            Boundary = textureFullBoundary;
            Index = arrayIndex;
        }
    }

    public readonly struct GlyphMetric : IGlyphMetrics {
        public readonly Vector2UInt StandardSize;
        public Vector2 Size { get; }
        public Vector2 Bearing { get; }

        internal GlyphMetric(Vector2UInt standardSize, Vector2 size, Vector2 bearing) {
            StandardSize = standardSize;
            Size = size;
            Bearing = bearing;
        }
    }
}