using Riptide.LowLevel.TextEngine.Harfbuzz;
using Riptide.ShaderCompilation;
using RiptideFoundation.Rendering;
using RiptideFoundation.Text;
using System.Reflection;

namespace RiptideEditorV2.UI;

public sealed unsafe class TextElement : VisualElement {
    private string? _text;
    private SignedDistanceFont _font = null!;
    private float _size = 24;
    
    public string? Text {
        get => _text;
        set {
            if (_text == value) return;

            _text = value;
            InvalidateGraphics();
        }
    }

    public SignedDistanceFont Font {
        get => _font;
        set {
            ArgumentNullException.ThrowIfNull(value, nameof(value));
            
            if (ReferenceEquals(_font, value)) return;

            _font = value;
            _font.IncrementReference();
            
            InvalidateGraphics();
        }
    }

    public float Size {
        get => _size;
        set {
            if (MathUtils.IsApproximate(_size, value, 0.01f)) return;

            _size = value;
            InvalidateGraphics();
        }
    }

    public override void BindMaterialProperties(MaterialProperties properties) {
        properties.SetTexture("_FontBitmap", _font.Bitmap);
    }

    public override int CalculateMaterialBatchingHash() {
        return _font.GetHashCode();
    }

    public override void GenerateMesh(MeshBuilder builder, Matrix3x2 transformation) {
        if (string.IsNullOrWhiteSpace(_text) || _size <= 0) return;
        
        Debug.Assert(_font != null, "_font != null");

        Matrix3x2 transform = Matrix3x2.CreateScale(_size / _font.Size) * transformation;
        
        var buffer = HarfbuzzApi.hb_buffer_create();
        
        try {
            var hbFont = _font.HarfbuzzFont;
            
            // Calculate font size.
            Vector2 textSize = Vector2.Zero;
            
            foreach (var line in _text.AsSpan().EnumerateLines()) {
                float horizontalSize = 0;
                
                if (line.IsEmpty || line.IsWhiteSpace()) goto nextLine;
                
                HarfbuzzApi.hb_buffer_clear_contents(buffer);

                fixed (char* pText = line) {
                    HarfbuzzApi.hb_buffer_add_utf16(buffer, pText, line.Length, 0, line.Length);
                }
                HarfbuzzApi.hb_buffer_guess_segment_properties(buffer);

                HarfbuzzApi.hb_shape(hbFont, buffer, null, 0);

                uint glyphCount;
                hb_glyph_position_t* positions = HarfbuzzApi.hb_buffer_get_glyph_positions(buffer, &glyphCount);

                for (uint i = 0; i < glyphCount; i++) {
                    var pos = positions[i];
                    horizontalSize += pos.xAdvance / 64f;
                }
                
                nextLine:
                textSize = new(float.Max(horizontalSize, textSize.X), textSize.Y + _font.LineGap);
            }
            
            Console.WriteLine(textSize);
            
            var pen = new Vector2(0, _font.Ascender);
            Span<ushort> indices = stackalloc ushort[6];
            
            foreach (var line in _text.AsSpan().EnumerateLines()) {
                if (line.IsEmpty || line.IsWhiteSpace()) goto nextLine;
                
                HarfbuzzApi.hb_buffer_clear_contents(buffer);

                fixed (char* pText = line) {
                    HarfbuzzApi.hb_buffer_add_utf16(buffer, pText, line.Length, 0, line.Length);
                }
                HarfbuzzApi.hb_buffer_guess_segment_properties(buffer);

                HarfbuzzApi.hb_shape(hbFont, buffer, null, 0);

                uint glyphCount;
                hb_glyph_info_t* info = HarfbuzzApi.hb_buffer_get_glyph_infos(buffer, &glyphCount);
                hb_glyph_position_t* positions = HarfbuzzApi.hb_buffer_get_glyph_positions(buffer, null);

                for (uint i = 0; i < glyphCount; i++) {
                    var glyphID = info[i].codepoint;
                    var pos = positions[i];
                    
                    if (_font.TryGetGlyphTextureInfo(glyphID, out var textureInfo) && _font.TryGetGlyphMetricInfo(glyphID, out var metricInfo)) {
                        var topLeft = pen + new Vector2(pos.xOffset, -pos.yOffset) / 64f + metricInfo.Bearing with {
                            Y = -metricInfo.Bearing.Y,
                        };
                        var size = metricInfo.StandardSize;
                        var bottomRight = topLeft + size;
                        
                        var bound = textureInfo.TextureStandardBoundary;
                        
                        var vcount = builder.GetWrittenVertexCount();
                        builder.WriteVertex(new Vertex(Vector2.Transform(topLeft, transform), bound.Min, Color32.White));
                        builder.WriteVertex(new Vertex(Vector2.Transform(new(bottomRight.X, topLeft.Y), transform), bound.Min with {
                            X = bound.Max.X
                        }, Color32.White));
                        builder.WriteVertex(new Vertex(Vector2.Transform(bottomRight, transform), bound.Max, Color32.White));
                        builder.WriteVertex(new Vertex(Vector2.Transform(new(topLeft.X, bottomRight.Y), transform), bound.Max with {
                            X = bound.Min.X
                        }, Color32.White));

                        indices[0] = indices[5] = (ushort)vcount;
                        indices[1] = (ushort)(vcount + 1);
                        indices[2] = indices[3] = (ushort)(vcount + 2);
                        indices[4] = (ushort)(vcount + 3);

                        builder.WriteIndices(indices);
                    }

                    pen += new Vector2(pos.xAdvance, pos.yAdvance) / 64f;
                }

                nextLine:
                pen = new(0, pen.Y + _font.LineGap);
            }
        } finally {
            HarfbuzzApi.hb_buffer_destroy(buffer);
        }
    }

    protected override void DisposeImpl(bool disposing) {
        base.DisposeImpl(disposing);

        _font.DecrementReference();
        _font = null!;
    }
}