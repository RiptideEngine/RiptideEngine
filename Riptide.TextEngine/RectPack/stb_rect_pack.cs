using System.Runtime.InteropServices;

namespace Riptide.LowLevel.TextEngine.RectPack;

public static unsafe partial class StbRectPack {
	[LibraryImport("stb_rect_pack")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool stbrp_pack_rects(stbrp_context* context, stbrp_rect* rects, int numRects);

	[LibraryImport("stb_rect_pack")]
	public static partial void stbrp_init_target(stbrp_context* context, int width, int height, stbrp_node* nodes, int numNodes);

	[LibraryImport("stb_rect_pack")]
	public static partial void stbrp_setup_allow_out_of_mem(stbrp_context* context, [MarshalAs(UnmanagedType.Bool)] bool allow_out_of_mem);

	[LibraryImport("stb_rect_pack")]
	public static partial void stbrp_setup_heuristic(stbrp_context* context, int heuristic);
}