﻿namespace RiptideEditor;

internal static unsafe partial class ImGuiInternal {
    public static void PushMultiItemsWidths(int components, float widthFull) => Native.igPushMultiItemsWidths(components, widthFull);
    public static void RenderTextEllipsis(ImDrawListPtr drawList, Vector2 posMin, Vector2 posMax, float clipMaxX, float ellipsisMaxX, ReadOnlySpan<char> text, ref Vector2 textSizeIfKnow) {
        int size = Encoding.UTF8.GetByteCount(text);

        if (size > 1024) {
            var pool = ArrayPool<byte>.Shared.Rent(size);
            Encoding.UTF8.GetBytes(text, pool);

            fixed (byte* ptr = pool) {
                Native.igRenderTextEllipsis(drawList, posMin, posMax, clipMaxX, ellipsisMaxX, ptr, ptr + size, (Vector2*)Unsafe.AsPointer(ref textSizeIfKnow));
            }

            ArrayPool<byte>.Shared.Return(pool);
        } else {
            Span<byte> utf8 = stackalloc byte[size];
            Encoding.UTF8.GetBytes(text, utf8);

            fixed (byte* ptr = utf8) {
                Native.igRenderTextEllipsis(drawList, posMin, posMax, clipMaxX, ellipsisMaxX, ptr, ptr + size, (Vector2*)Unsafe.AsPointer(ref textSizeIfKnow));
            }
        }
    }
    public static void RenderTextWrapped(Vector2 pos, ReadOnlySpan<char> text, float wrapWidth) {
        int size = Encoding.UTF8.GetByteCount(text);

        if (size > 1024) {
            var pool = ArrayPool<byte>.Shared.Rent(size);
            Encoding.UTF8.GetBytes(text, pool);

            fixed (byte* ptr = pool) {
                Native.igRenderTextWrapped(pos, ptr, ptr + size, wrapWidth);
            }

            ArrayPool<byte>.Shared.Return(pool);
        } else {
            Span<byte> utf8 = stackalloc byte[size];
            Encoding.UTF8.GetBytes(text, utf8);

            fixed (byte* ptr = utf8) {
                Native.igRenderTextWrapped(pos, ptr, ptr + size, wrapWidth);
            }
        }
    }
    public static bool ButtonBehavior(Bound2D bb, uint id, out bool hovered, out bool held, ImGuiButtonFlags flags) {
        fixed (bool* pHovered = &hovered) {
            fixed (bool* pHeld = &held) {
                return Native.igButtonBehavior(&bb, id, pHovered, pHeld, flags);
            }
        }
    }

    public static bool ImageButtonEx(uint id, nint textureID, Vector2 size, Vector2 uv0, Vector2 uv1, Vector4 bgColor, Vector4 tintColor, ImGuiButtonFlags flags) => Native.igImageButtonEx(id, textureID, size, uv0, uv1, bgColor, tintColor, flags);

    public static partial class Native {
        [LibraryImport("cimgui")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void igPushMultiItemsWidths(int components, float widthFull);

        [DllImport("cimgui", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igRenderTextEllipsis(ImDrawListPtr drawList, Vector2 posMin, Vector2 posMax, float clipMaxX, float ellipsisMaxX, byte* text, byte* textEnd, Vector2* textSizeIfKnow);

        [DllImport("cimgui", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igRenderTextWrapped(Vector2 pos, byte* text, byte* text_end, float wrap_width);

        [LibraryImport("cimgui")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(System.Runtime.InteropServices.UnmanagedType.U1)]
        public static partial bool igButtonBehavior(Bound2D* bb, uint id, bool* outHovered, bool* oldHeld, ImGuiButtonFlags flags);

        [DllImport("cimgui", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(System.Runtime.InteropServices.UnmanagedType.U1)]
        public static extern bool igImageButtonEx(uint id, nint textureID, Vector2 size, Vector2 uv0, Vector2 uv1, Vector4 bgColor, Vector4 tintColor, ImGuiButtonFlags flags);
    }
}