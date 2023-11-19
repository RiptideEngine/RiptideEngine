using Riptide.LowLevel.TextEngine.FreeType;

namespace Riptide.LowLevel.TextEngine; 

public static unsafe class FontEngine {
    private static FT_Library _library;

    public static void Initialize() {
        if (_library.Handle != nint.Zero) throw new InvalidOperationException("Font Engine has already initialized.");

        FreeTypeBinding.FT_Init_FreeType(out _library);
    }
    
    public static void Shutdown() {
        if (_library.Handle == nint.Zero) throw new InvalidOperationException("Font Engine has already shutdown.");

        FreeTypeBinding.FT_Done_FreeType(_library);
    }

    public static void EnsureInitialized() {
        if (_library.Handle == nint.Zero) throw new InvalidOperationException("Font Engine must be initialized before this operation.");
    }

    public static FT_Face LoadFace(string path, int faceIndex = 0) {
        var error = FreeTypeBinding.FT_New_Face(_library, path, faceIndex, out var face);
        if (error.IsError) throw new ArgumentException($"Failed to load face from path '{path}' (Error code: {error.Code}).");

        return face;
    }

    public static FT_Face LoadFace(ReadOnlySpan<byte> memory, int faceIndex) {
        fixed (byte* pMemory = memory) {
            var error = FreeTypeBinding.FT_New_Memory_Face(_library, pMemory, memory.Length, faceIndex, out var face);
            if (error.IsError) throw new ArgumentException($"Failed to load face from memory (Error code: {error.Code}).");

            return face;
        }
    }
}