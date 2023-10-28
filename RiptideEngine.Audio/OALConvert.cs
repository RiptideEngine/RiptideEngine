namespace RiptideEngine.Audio;

internal static class OALConvert {
    public static bool TryConvert(AudioFormat input, out BufferFormat output) {
        switch (input) {
            case AudioFormat.Mono8: output = BufferFormat.Mono8; return true;
            case AudioFormat.Stereo8: output = BufferFormat.Stereo8; return true;
            case AudioFormat.Mono16: output = BufferFormat.Mono16; return true;
            case AudioFormat.Stereo16: output = BufferFormat.Stereo16; return true;
            case AudioFormat.MonoFloat32: output = (BufferFormat)0x10010; return true;
            case AudioFormat.StereoFloat32: output = (BufferFormat)0x10011; return true;
            case AudioFormat.MonoFloat64: output = (BufferFormat)0x10012; return true;
            case AudioFormat.StereoFloat64: output = (BufferFormat)0x10013; return true;
            default: output = default; return false;
        }
    }
}