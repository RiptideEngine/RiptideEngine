namespace Riptide.LowLevel.TextEngine.Harfbuzz;

public struct hb_glyph_info_t {
    public uint codepoint;
    private uint mask;
    public uint cluster;
    private hb_var_int_t var1;
    private hb_var_int_t var2;
}