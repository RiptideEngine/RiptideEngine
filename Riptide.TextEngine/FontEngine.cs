using Riptide.LowLevel.TextEngine.FreeType;

namespace Riptide.LowLevel.TextEngine;

public static unsafe class FontEngine {
    private static FT_Library _library;
    private static object _faceCreationLock;

    public static void Initialize() {
        if (_library.Handle != 0) throw new InvalidOperationException("Font engine is already initialized.");

        var error = FreeTypeApi.FT_Init_FreeType(out _library);
        if (error.IsError) throw new("Failed to initialize FreeType.");

        _faceCreationLock = new();
    }

    public static void Shutdown() {
        if (_library.Handle == 0) return;

        FreeTypeApi.FT_Done_FreeType(_library);
        _library = default;
    }

    internal static void EnsureInitialized() {
        if (_library.Handle == 0) throw new("FontEngine must be initialized first.");
    }

    public static FT_Error CreateFace(string path, int faceIndex, out FT_FaceRec* pFace) {
        EnsureInitialized();

        lock (_faceCreationLock) {
            return FreeTypeApi.FT_New_Face(_library, path, faceIndex, out pFace);
        }
    }

    public static FT_Error CreateFace(ReadOnlySpan<byte> memory, int faceIndex, out FT_FaceRec* pFace) {
        EnsureInitialized();

        lock (_faceCreationLock) {
            fixed (byte* pMemory = memory) {
                return FreeTypeApi.FT_New_Memory_Face(_library, pMemory, memory.Length, faceIndex, out pFace);
            }
        }
    }

    public static void FreeFace(FT_FaceRec* pFace) {
        lock (_faceCreationLock) {
            FreeTypeApi.FT_Done_Face(pFace);
        }
    }
}