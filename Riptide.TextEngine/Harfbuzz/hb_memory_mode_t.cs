namespace Riptide.LowLevel.TextEngine.Harfbuzz;

public enum hb_memory_mode_t {
    Duplicate,
    Readonly,
    Writeonly,
    ReadonlyMayMakeWritable,
}