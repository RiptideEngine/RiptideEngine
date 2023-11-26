using Riptide.LowLevel.TextEngine;
using Riptide.LowLevel.TextEngine.FreeType;
using Riptide.LowLevel.TextEngine.RectPack;
using System.Text;

namespace RiptideFoundation;

public sealed unsafe class Font : RiptideRcObject {
    public Texture2D Bitmap { get; }
    private readonly InformationTable _codepointInfos;
    public uint Size { get; }
    public float Height { get; }
    
    public override string? Name {
        get => base.Name;
        set {
            base.Name = value;

            if (Bitmap != null) {
                Bitmap.Name = $"{value}.Bitmap";
            }
        }
    }

    private Font(Texture2D bitmap, InformationTable informationTable, uint size, float height) {
        Bitmap = bitmap;
        _codepointInfos = informationTable;
        Size = size;
        Height = height;

        _refcount = 1;
    }

    public bool ContainsCodepoint(int codepoint) => _codepointInfos.ContainsKey(codepoint);
    public bool TryGetCodepointInformation(int codepoint, out CodepointInformation output) => _codepointInfos.TryGetValue(codepoint, out output);

    protected override void Dispose() {
        Bitmap.DecrementReference();
        _codepointInfos.Clear();
    }

    public static Font? Import(string path, Vector2Int bitmapDimension, uint fontSize, ReadOnlySpan<CodepointRange> codepointRanges) {
        if (codepointRanges.IsEmpty) return null;
        
        FontEngine.EnsureInitialized();

        var face = FontEngine.LoadFace(path);

        try {
            InformationTable informationTable = new();
            Texture2D bitmap = new((ushort)bitmapDimension.X, (ushort)bitmapDimension.Y, GraphicsFormat.R8UNorm);
            byte[] pixels = new byte[bitmapDimension.X * bitmapDimension.Y];

            try {
                FreeTypeBinding.FT_Set_Pixel_Sizes(face, 0, fontSize);
                List<stbrp_rect> rects = new();

                stbrp_context stbContext;
                var nodes = new stbrp_node[bitmapDimension.X];

                fixed (stbrp_node* pNodes = nodes) {
                    StbRectPack.stbrp_init_target(&stbContext, bitmapDimension.X, bitmapDimension.Y, pNodes, nodes.Length);
                }

                foreach (var range in codepointRanges) {
                    for (int i = range.Start; i <= range.End; i++) {
                        uint glyphIndex = FreeTypeBinding.FT_Get_Char_Index(face, (uint)i);

                        if (glyphIndex == 0) continue;

                        var error = FreeTypeBinding.FT_Load_Glyph(face, glyphIndex, FT_Load_Flags.Default);
                        if (error.IsError) continue;

                        error = FreeTypeBinding.FT_Render_Glyph(face.Handle->Glyph, FT_Render_Mode.Normal);
                        if (error.IsError) continue;
                        
                        error = FreeTypeBinding.FT_Render_Glyph(face.Handle->Glyph, FT_Render_Mode.Sdf);
                        if (error.IsError) continue;
                        
                        rects.Add(new() {
                            ID = i,
                            Width = (int)face.Handle->Glyph.Handle->Bitmap.Width,
                            Height = (int)face.Handle->Glyph.Handle->Bitmap.Rows,
                        });
                    }
                }
                
                fixed (stbrp_rect* pRects = CollectionsMarshal.AsSpan(rects)) {
                    bool pack = StbRectPack.stbrp_pack_rects(&stbContext, pRects, rects.Count);
                    if (!pack) {
                        Console.WriteLine("Not all rectangles are packed successfully.");
                    }

                    var bitmapDenominator = bitmapDimension - Vector2.One;

                    foreach (ref readonly var rect in CollectionsMarshal.AsSpan(rects)) {
                        if (!rect.WasPacked) continue;

                        var codepoint = rect.ID;
                        
                        uint glyphIndex = FreeTypeBinding.FT_Get_Char_Index(face, (uint)codepoint);
                        if (glyphIndex == 0) continue;

                        var error = FreeTypeBinding.FT_Load_Glyph(face, glyphIndex, FT_Load_Flags.Default);
                        if (error.IsError) continue;

                        error = FreeTypeBinding.FT_Render_Glyph(face.Handle->Glyph, FT_Render_Mode.Normal);
                        if (error.IsError) continue;
                        
                        error = FreeTypeBinding.FT_Render_Glyph(face.Handle->Glyph, FT_Render_Mode.Sdf);
                        if (error.IsError) continue;
                        
                        var glyphBitmap = face.Handle->Glyph.Handle->Bitmap;
                        
                        // Copy bitmap.
                        for (uint row = 0, end = glyphBitmap.Rows; row < end; row++) {
                            var src = glyphBitmap.Buffer + glyphBitmap.Pitch * row;
                            var dst = pixels.AsSpan((int)(bitmapDimension.X * rect.Y + rect.X + bitmapDimension.X * row), (int)glyphBitmap.Width);
                        
                            new ReadOnlySpan<byte>(src, (int)glyphBitmap.Width).CopyTo(dst);
                        }

                        // Convert metric information.
                        ref readonly var glyph = ref face.Handle->Glyph;
                        ref readonly var metrics = ref glyph.Handle->Metrics;
                        
                        var size = new Vector2(glyphBitmap.Width, glyphBitmap.Rows);
                        var hbearing = new Vector2(metrics.HoriBearingX.Value, metrics.HoriBearingY.Value) / 64f;
                        var advance = new Vector2(glyph.Handle->Advance.X.Value, glyph.Handle->Advance.Y.Value) / 64f;

                        var uv0 = new Vector2(rect.X / bitmapDenominator.X, rect.Y / bitmapDenominator.Y);
                        var uv1 = new Vector2((rect.X + rect.Width - 1) / bitmapDenominator.X, (rect.Y + rect.Height) / bitmapDenominator.Y);
                        
                        informationTable.Add(codepoint, new(new(size, hbearing, advance), new(uv0, uv1)));
                    }
                }

                {
                    var context = Graphics.RenderingContext;
                    var cmdList = context.Factory.CreateCommandList();
                    
                    cmdList.TranslateState(bitmap.UnderlyingTexture, ResourceTranslateStates.CopyDestination);

                    cmdList.UpdateResource(bitmap.UnderlyingTexture, pixels);
                    
                    cmdList.TranslateState(bitmap.UnderlyingTexture, ResourceTranslateStates.ShaderResource);
                    
                    cmdList.Close();
                    context.ExecuteCommandList(cmdList);
                    context.WaitForGpuIdle();
                    cmdList.DecrementReference();
                }
            } catch {
                bitmap.DecrementReference();
                throw;
            }

            return new(bitmap, informationTable, fontSize, (float)face.Handle->Size.Handle->Metrics.Height.Value / 64);
        } finally {
            FreeTypeBinding.FT_Done_Face(face);
        }
    }

    private sealed class InformationTable : Dictionary<int, CodepointInformation>;

    public readonly record struct CodepointInformation(CodepointMetric Metric, Bound2D TextureBoundary);
    public readonly record struct CodepointMetric(Vector2 Size, Vector2 Bearing, Vector2 Advance);
}

public readonly struct CodepointRange {
    public readonly int Start, End;

    public CodepointRange(int start, int end) {
        Start = start;
        End = end;
    }

    public CodepointRange(Rune start, Rune end) {
        Start = start.Value;
        End = end.Value;
    }

    public CodepointRange(string startCharacter, string endCharacter) {
        Start = char.ConvertToUtf32(startCharacter, 0);
        End = char.ConvertToUtf32(endCharacter, 0);
    }
}