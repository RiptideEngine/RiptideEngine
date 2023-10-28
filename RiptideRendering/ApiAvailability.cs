namespace RiptideRendering;

/// <summary>
/// Static helper to check whether a rendering API is available to be used.
/// </summary>
internal static unsafe class ApiAvailability {
    public static bool IsDirect3D12Available() {
        if (!OperatingSystem.IsWindows()) return false;
        if (!NativeLibrary.TryLoad("D3D12.dll", out nint library)) return false;

        try {
            if (!NativeLibrary.TryGetExport(library, "D3D12CreateDevice", out nint export)) return false;

            Guid guid = new(0x189819f1, 0x1db6, 0x4b57, 0xbe, 0x54, 0x18, 0x21, 0x33, 0x9b, 0x85, 0xf7);
            return ((delegate* unmanaged[Cdecl]<void*, int, Guid*, void**, int>)export)(null, 0xb000, &guid, null) >= 0;
        } finally {
            NativeLibrary.Free(library);
        }
    }
}