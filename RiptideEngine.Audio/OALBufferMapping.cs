namespace RiptideEngine.Audio {
    [Flags]
    internal enum BufferMapFlags {
        Read = 1,
        Write = 2,
        Persistent = 4,
        PreserveData = 8,
    }

    internal static unsafe class OALBufferMapping {
        public static delegate* unmanaged<uint, BufferFormat, void*, int, int, BufferMapFlags, void> alBufferStorageSOFT;
        public static delegate* unmanaged<uint, int, int, BufferMapFlags, void*> alMapBufferSOFT;
        public static delegate* unmanaged<uint, void> alUnmapBufferSOFT;

        public static void GetFunctionPointers(AL api) {
            alBufferStorageSOFT = (delegate* unmanaged<uint, BufferFormat, void*, int, int, BufferMapFlags, void>)api.GetProcAddress("alBufferStorageSOFT");
            alMapBufferSOFT = (delegate* unmanaged<uint, int, int, BufferMapFlags, void*>)api.GetProcAddress("alMapBufferSOFT");
            alUnmapBufferSOFT = (delegate* unmanaged<uint, void>)api.GetProcAddress("alUnmapBufferSOFT");
        }
    }
}
