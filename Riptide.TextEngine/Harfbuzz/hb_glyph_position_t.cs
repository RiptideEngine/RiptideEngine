namespace Riptide.LowLevel.TextEngine.Harfbuzz;

public struct hb_glyph_position_t {
    public int xAdvance;
    public int yAdvance;
    public int xOffset;
    public int yOffset;

    /*< private >*/
    private hb_var_int_t   var;
}