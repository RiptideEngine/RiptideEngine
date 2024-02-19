using RectpackSharp;
using Riptide.LowLevel.TextEngine;
using Riptide.LowLevel.TextEngine.FreeType;
using Riptide.LowLevel.TextEngine.Harfbuzz;
using RiptideFoundation.Rendering;

namespace RiptideFoundation.Text;

partial class SignedDistanceFont {
    public static Builder CreateBuilder() => new();
    
    public sealed unsafe class Builder {
        private readonly List<CodepointCandidate> _candidates;
        private Vector2UInt _bitmapDimension;
        private uint _size;
        private FontDataSource _source;

        internal Builder() {
            _candidates = [];
            _bitmapDimension = new(1024);
            _size = 14;
        }

        public Builder ImportFromFile(string path) {
            _source = new(path);
            return this;
        }

        public Builder ImportFromMemory(byte[] memory) {
            _source = new(memory);
            return this;
        }

        public Builder AddRasterizeCandidate(CodepointCandidate candidate) {
            _candidates.Add(candidate);
            return this;
        }

        public Builder SetBitmapDimension(Vector2UInt size) {
            _bitmapDimension = Vector2UInt.Clamp(size, new(128), new(Graphics.RenderingContext.Capability.GetMaximumTextureSize(TextureDimension.Texture2D).Dimension));
            return this;
        }

        public Builder SetFontSize(uint size) {
            _size = uint.Max(size, 3);
            return this;
        }

        public SignedDistanceFont Build() {
            FT_Error error;
            FT_FaceRec* pFace;

            switch (_source.Type) {
                case FontDataSource.SourceType.Unknown: throw new InvalidOperationException("Importing location is currently undefined.");
                case FontDataSource.SourceType.File:
                    error = FontEngine.CreateFace(_source.FilePath, 0, out pFace);
                    if (error.IsError) throw new($"Failed to import face at file path '{_source.FilePath}'.");
                    break;
                
                case FontDataSource.SourceType.Memory:
                    error = FontEngine.CreateFace(_source.Memory, 0, out pFace);
                    if (error.IsError) throw new("Failed to import face from given memory.");
                    break;
                
                default: throw new UnreachableException();
            }

            return BuildFont(pFace);

            SignedDistanceFont BuildFont(FT_FaceRec* face) {
                Texture2D bitmap = new(_bitmapDimension.X, (ushort)_bitmapDimension.Y, GraphicsFormat.R8UNorm);
                byte[] pixels = new byte[_bitmapDimension.X * _bitmapDimension.Y];
                
                GlyphTextureTable<GlyphTexture> glyphTextureTable = new();
                GlyphMetricTable<GlyphMetric> glyphMetricTable = new();
                
                HashSet<int> distinctSet = [];

                try {
                    FreeTypeApi.FT_Set_Pixel_Sizes(face, 0, _size);
                    List<PackingRectangle> packingRects = new(256);
                    
                    // Add notdef character
                    {
                        FreeTypeApi.FT_Load_Glyph(pFace, 0, FT_Load_Flags.NoBitmap);
                        FreeTypeApi.FT_Render_Glyph(pFace->Glyph, FT_Render_Mode.Normal);
                        FreeTypeApi.FT_Render_Glyph(pFace->Glyph, FT_Render_Mode.Sdf);
                    
                        packingRects.Add(new() {
                            Id = 0,
                            Width = pFace->Glyph->Bitmap.Width,
                            Height = pFace->Glyph->Bitmap.Rows,
                        });
                    }

                    foreach (var candidate in _candidates) {
                        foreach (var codepoint in candidate) {
                            if (!distinctSet.Add(codepoint)) continue;
                            
                            var glyphIndex = FreeTypeApi.FT_Get_Char_Index(pFace, (uint)codepoint);
                            if (glyphIndex == 0) continue;

                            error = FreeTypeApi.FT_Load_Glyph(pFace, glyphIndex, FT_Load_Flags.NoBitmap);
                            if (error.IsError) continue;
                    
                            ref readonly var glyph = ref pFace->Glyph;

                            error = FreeTypeApi.FT_Render_Glyph(glyph, FT_Render_Mode.Normal);
                            if (error.IsError) continue;
                    
                            if (glyph->Bitmap.Width == 0 || glyph->Bitmap.Rows == 0) {
                                ref var cvtMetric = ref CollectionsMarshal.GetValueRefOrAddDefault(glyphMetricTable, glyphIndex, out bool exists);
                                if (!exists) {
                                    cvtMetric = new(Vector2UInt.Zero, Vector2UInt.Zero, Vector2.Zero);
                                }

                                continue;
                            }
                        
                            error = FreeTypeApi.FT_Render_Glyph(glyph, FT_Render_Mode.Sdf);
                            if (error.IsError) continue;

                            packingRects.Add(new() {
                                Id = (int)glyphIndex,
                                Width = glyph->Bitmap.Width,
                                Height = glyph->Bitmap.Rows,
                            });
                        }
                    }
                    
                    PackingRectangle[] rectArray = packingRects.ToArray();
                    
                    try {
                        RectanglePacker.Pack(rectArray, out _, PackingHints.FindBest, 1, 1, _bitmapDimension.X, _bitmapDimension.Y);
                    } catch (Exception e) {
                        throw new("Failed to pack character bitmap.", e);
                    }
                    
                    Vector2 bitmapDenominator = _bitmapDimension;
                    
                    foreach (ref readonly var rect in rectArray.AsSpan()) {
                        var glyphIndex = (uint)rect.Id;
                    
                        if (glyphTextureTable.ContainsKey(glyphIndex)) continue;
                        
                        error = FreeTypeApi.FT_Load_Glyph(face, glyphIndex, FT_Load_Flags.Default);
                        if (error.IsError) continue;
                    
                        var glyph = face->Glyph;

                        error = FreeTypeApi.FT_Render_Glyph(glyph, FT_Render_Mode.Normal);
                        if (error.IsError) continue;
                        
                        var glyphBitmap = glyph->Bitmap;
                        
                        var rasterizeBitmapWidth = glyphBitmap.Width;
                        var rasterizeBitmapHeight = glyphBitmap.Rows;
                        
                        error = FreeTypeApi.FT_Render_Glyph(glyph, FT_Render_Mode.Sdf);
                        if (error.IsError) continue;
                        
                        glyphBitmap = glyph->Bitmap;
                        
                        var sdfBitmapWidth = glyphBitmap.Width;
                        var sdfBitmapHeight = glyphBitmap.Rows;
                        
                        // Copy bitmap.
                        for (uint row = 0; row < sdfBitmapHeight; row++) {
                            //var src = glyphBitmap.Buffer + glyphBitmap.Pitch * row;
                            var src = glyphBitmap.Buffer + glyphBitmap.Pitch * row;
                            var dst = pixels.AsSpan((int)(_bitmapDimension.X * rect.Y + rect.X + _bitmapDimension.X * row), (int)sdfBitmapWidth);
                        
                            new ReadOnlySpan<byte>(src, (int)sdfBitmapWidth).CopyTo(dst);
                        }
                        
                        // Convert metric information.
                        var fulluv0 = new Vector2(rect.X, rect.Y) / bitmapDenominator;
                        var fulluv1 = fulluv0 + new Vector2(rect.Width, rect.Height) / bitmapDenominator;
                        
                        var stduv0 = (new Vector2(rect.X, rect.Y) + new Vector2(glyphBitmap.Width - rasterizeBitmapWidth, glyphBitmap.Rows - rasterizeBitmapHeight) / 2) / bitmapDenominator;
                        var stduv1 = stduv0 + new Vector2(rasterizeBitmapWidth, rasterizeBitmapHeight) / bitmapDenominator;
                        
                        var bearing = new Vector2(face->Glyph->Metrics.HoriBearingX.Value, face->Glyph->Metrics.HoriBearingY.Value) / 64f;
                        
                        glyphMetricTable.Add(glyphIndex, new(new((uint)glyph->Metrics.Width.Value >> 6, (uint)glyph->Metrics.Height.Value >> 6), new(sdfBitmapWidth, sdfBitmapHeight), bearing));
                        glyphTextureTable.Add(glyphIndex, new(new(stduv0, stduv1), new(fulluv0, fulluv1), 0));
                    }
                } catch {
                    bitmap.DecrementReference();
                    throw;
                }

                var cmdList = Graphics.RenderingContext.Factory.CreateCommandList();
                cmdList.TranslateResourceState(bitmap.UnderlyingTexture, ResourceTranslateStates.CopyDestination);
                cmdList.UpdateTexture(bitmap.UnderlyingTexture, 0, pixels);
                cmdList.TranslateResourceState(bitmap.UnderlyingTexture, ResourceTranslateStates.ShaderResource);
                cmdList.Close();

                Graphics.RenderingContext.Synchronizer.WaitCpu(Graphics.RenderingContext.ExecuteCommandList(cmdList));
                cmdList.DecrementReference();
                
                return new(pFace, HarfbuzzApi.hb_ft_font_create(pFace, null), glyphTextureTable, glyphMetricTable, bitmap, _size);
            }
        }
    }
}