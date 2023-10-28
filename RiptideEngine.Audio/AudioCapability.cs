﻿namespace RiptideEngine.Audio;

internal static class AudioCapability {
    private static bool _floatFormatSupported;
    private static bool _doubleFormatSupported;

    internal static void DoCapabilityCheck() {
        Debug.Assert(AudioEngine.AL != null);

        _floatFormatSupported = AudioEngine.AL!.IsExtensionPresent("AL_EXT_FLOAT32");
        _doubleFormatSupported = AudioEngine.AL!.IsExtensionPresent("AL_EXT_DOUBLE");
    }

    public static bool IsFormatSupported(AudioFormat format) {
        if (!AudioEngine.IsInitialized()) return false;

        return format switch {
            AudioFormat.Mono8 or AudioFormat.Stereo8 or AudioFormat.Mono16 or AudioFormat.Stereo16 => true,
            AudioFormat.MonoFloat32 or AudioFormat.StereoFloat32 => _floatFormatSupported,
            AudioFormat.MonoFloat64 or AudioFormat.StereoFloat64 => _doubleFormatSupported,
            _ => false,
        };
    }
}