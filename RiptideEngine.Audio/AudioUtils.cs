namespace RiptideEngine.Audio;

public static class AudioUtils {
    public static (uint Bitdepth, uint NumChannels) GetBitdepthAndNumChannels(AudioFormat format) {
        return format switch {
            AudioFormat.Mono8 => (8, 1),
            AudioFormat.Stereo8 => (8, 2),
            AudioFormat.Mono16 => (16, 1),
            AudioFormat.Stereo16 => (16, 2),
            AudioFormat.MonoFloat32 => (32, 1),
            AudioFormat.StereoFloat32 => (32, 2),
            _ => default,
        };
    }

    public static uint GetNumChannels(AudioFormat format) {
        return format switch {
            AudioFormat.Mono8 or AudioFormat.Mono16 or AudioFormat.MonoFloat32 => 1,
            AudioFormat.Stereo8 or AudioFormat.Stereo16 or AudioFormat.StereoFloat32 => 2,
            _ => 0,
        };
    }

    public static uint AlignByte(uint byteLength, AudioFormat format) {
        return format switch {
            AudioFormat.Mono8 => byteLength,
            AudioFormat.Stereo8 or AudioFormat.Mono16 => byteLength & ~1U,
            AudioFormat.Stereo16 or AudioFormat.MonoFloat32 => byteLength & ~3U,
            AudioFormat.StereoFloat32 => byteLength & ~7U,

            _ => 0,
        };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static uint AlignByte(uint byteLength, uint bitdepth, uint channels) {
        return byteLength & ~((bitdepth >> 3) * channels - 1);
    }

    public static T LinearToDecibel<T>(T value) where T : IFloatingPointIeee754<T> => T.CreateChecked(8.68588963806503655302) * T.Log(value);
    public static T DecibelToLinear<T>(T value) where T : IFloatingPointIeee754<T> => T.Exp(value * T.CreateChecked(0.11512925464970228420089957273422));

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public static uint SampleToByte(uint sample, uint bitdepth, uint channels) => sample * (bitdepth >> 3) * channels;
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] public static uint ByteToSample(uint @byte, uint bitdepth, uint channels) => @byte / channels / (bitdepth >> 3);
    
}