using Riptide.LowLevel.TextEngine.FreeType;

namespace Riptide.LowLevel.TextEngine.Harfbuzz;

#pragma warning disable CA1401
public static unsafe partial class HarfbuzzApi {
    private const string LibraryName = "harfbuzz.dll";

    // Creation/Destroy
    [LibraryImport(LibraryName)]
    public static partial hb_blob_t* hb_blob_create(byte* data, uint length, hb_memory_mode_t mode, void* userData, hb_destroy_func_t destroy);
    
    [LibraryImport(LibraryName)]
    public static partial hb_blob_t* hb_blob_create_or_fail(byte* data, uint length, hb_memory_mode_t mode, void* userData, hb_destroy_func_t destroy);

    [LibraryImport(LibraryName)]
    public static partial hb_blob_t* hb_blob_create_from_file([MarshalAs(UnmanagedType.LPStr)] string fileName);
    
    [LibraryImport(LibraryName)]
    public static partial hb_blob_t* hb_blob_create_from_file(byte* fileName);
    
    [LibraryImport(LibraryName)]
    public static partial hb_blob_t* hb_blob_create_from_file_or_fail(byte* fileName);
    
    [LibraryImport(LibraryName)]
    public static partial void hb_blob_destroy(hb_blob_t* blob);
    
    [LibraryImport(LibraryName)]
    public static partial hb_face_t* hb_face_create(hb_blob_t* blob, uint index);
    
    [LibraryImport(LibraryName)]
    public static partial void hb_face_destroy(hb_face_t* face);
    
    [LibraryImport(LibraryName)]
    public static partial hb_font_t* hb_font_create(hb_face_t* face);
    
    [LibraryImport(LibraryName)]
    public static partial hb_font_t* hb_font_create_sub_font(hb_font_t* parent);
    
    [LibraryImport(LibraryName)]
    public static partial hb_font_t* hb_ft_font_create(FT_FaceRec* face, hb_destroy_func_t? destroyFunc);

    [LibraryImport(LibraryName)]
    public static partial void hb_font_destroy(hb_font_t* font);
    
    [LibraryImport(LibraryName)]
    public static partial hb_buffer_t* hb_buffer_create();
    
    [LibraryImport(LibraryName)]
    public static partial void hb_buffer_destroy(hb_buffer_t* buffer);

    // General
    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool hb_font_get_glyph(hb_font_t* font, uint unicode, uint variationSelector, uint* glyph);
    
    [LibraryImport(LibraryName)]
    public static partial hb_face_t* hb_font_get_face(hb_font_t* font);
    
    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool hb_font_get_glyph_name(hb_font_t* font, uint codepoint, byte* outputName, uint bufferSize);

    [LibraryImport(LibraryName)]
    public static partial void hb_font_set_scale(hb_font_t* font, int xScale, int yScale);
    
    [LibraryImport(LibraryName)]
    public static partial uint hb_blob_get_length(hb_blob_t* blob);

    [LibraryImport(LibraryName)]
    public static partial byte* hb_blob_get_data(hb_blob_t* blob, out uint length);

    [LibraryImport(LibraryName)]
    public static partial void hb_buffer_clear_contents(hb_buffer_t* buffer);
    
    [LibraryImport(LibraryName)]
    public static partial void hb_buffer_add(hb_buffer_t* buffer, uint codepoint, uint cluster);

    [LibraryImport(LibraryName)]
    public static partial void hb_buffer_add_utf8(hb_buffer_t* buffer, byte* text, int length, uint itemOffset, int itemLength);
    
    [LibraryImport(LibraryName)]
    public static partial void hb_buffer_add_utf16(hb_buffer_t* buffer, char* text, int length, uint itemOffset, int itemLength);
    
    [LibraryImport(LibraryName)]
    public static partial void hb_buffer_add_utf32(hb_buffer_t* buffer, uint* text, int length, uint itemOffset, int itemLength);
    
    [LibraryImport(LibraryName)]
    public static partial void hb_buffer_add_codepoints(hb_buffer_t* buffer, uint* text, int length, uint itemOffset, int itemLength);
    
    [LibraryImport(LibraryName)]
    public static partial void hb_buffer_append(hb_buffer_t* buffer, hb_buffer_t* source, uint start, uint end);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool hb_buffer_set_length(hb_buffer_t  *buffer, uint length);

    [LibraryImport(LibraryName)]
    public static partial uint hb_buffer_get_length(hb_buffer_t* buffer);
    
    [LibraryImport(LibraryName)]
    public static partial hb_glyph_info_t* hb_buffer_get_glyph_infos(hb_buffer_t* buffer, uint* length);

    [LibraryImport(LibraryName)]
    public static partial hb_glyph_position_t* hb_buffer_get_glyph_positions(hb_buffer_t* buffer, uint* length);

    [LibraryImport(LibraryName)]
    public static partial void hb_buffer_guess_segment_properties(hb_buffer_t* buffer);

    [LibraryImport(LibraryName)]
    public static partial void hb_shape(hb_font_t* font, hb_buffer_t* buffer, hb_feature_t* features, uint numFeatures);
    
    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool hb_shape_full(hb_font_t* font, hb_buffer_t* buffer, hb_feature_t* features, uint numFeatures, char* shapersList);
    
    [LibraryImport(LibraryName)] 
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool hb_shape_justify (hb_font_t* font, hb_buffer_t* buffer, hb_feature_t* features, uint numFeatures, char* shapersList, float minTargetAdvance, float maxTargetAdvance, ref float advance, out uint varTag, out float varValue);
    
    [LibraryImport(LibraryName)] 
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool hb_ot_metrics_get_position(hb_font_t* font, hb_ot_metrics_tag_t metricTag, int* position);
    
    [LibraryImport(LibraryName)] 
    public static partial void hb_ot_metrics_get_position_with_fallback(hb_font_t* font, hb_ot_metrics_tag_t metricTag, int* position);
    
    [LibraryImport(LibraryName)] 
    public static partial float hb_ot_metrics_get_variation(hb_font_t* font, hb_ot_metrics_tag_t metricTag);
    
    [LibraryImport(LibraryName)] 
    public static partial int hb_ot_metrics_get_x_variation(hb_font_t* font, hb_ot_metrics_tag_t metricTag);
    
    [LibraryImport(LibraryName)]
    public static partial int hb_ot_metrics_get_y_variation(hb_font_t* font, hb_ot_metrics_tag_t metricTag);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool hb_ot_math_has_data(hb_face_t* face);
}
#pragma warning restore CA1401
