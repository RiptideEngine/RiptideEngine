namespace Riptide.LowLevel.TextEngine.Harfbuzz;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct hb_var_int_t {
    [FieldOffset(0)] public uint u32;
    [FieldOffset(0)] public int i32;
    [FieldOffset(0)] public fixed ushort u16[2];
    [FieldOffset(0)] public fixed short i16[2];
    [FieldOffset(0)] public fixed byte u8[4];
    [FieldOffset(0)] public fixed sbyte i8[4];
}