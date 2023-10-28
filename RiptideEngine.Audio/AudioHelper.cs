namespace RiptideEngine.Audio;

public static class AudioHelper {
    /*
     *  Terminology:
     *      - A sample is a value or set of values of the audio source per channel, which make up sound that we heard.
     *      - A frame is a set of samples in all channels. For example, stereo audio has 2 samples per frame.
     */

    public static (uint Bitdepth, uint NumChannels) GetBitdepthAndNumChannels(AudioFormat format) {
        return format switch {
            AudioFormat.Mono8 => (8, 1),
            AudioFormat.Stereo8 => (8, 2),
            AudioFormat.Mono16 => (16, 1),
            AudioFormat.Stereo16 => (16, 2),
            AudioFormat.MonoFloat32 => (32, 1),
            AudioFormat.StereoFloat32 => (32, 2),
            AudioFormat.MonoFloat64 => (64, 1),
            AudioFormat.StereoFloat64 => (64, 2),
            _ => default,
        };
    }

    public static uint AlignAudioLength(uint byteLength, AudioFormat format) {
        return format switch {
            AudioFormat.Mono8 => byteLength,
            AudioFormat.Stereo8 or AudioFormat.Mono16 => byteLength & ~1U,
            AudioFormat.Stereo16 or AudioFormat.MonoFloat32 => byteLength & ~3U,
            AudioFormat.StereoFloat32 or AudioFormat.MonoFloat64 => byteLength & ~7U,
            AudioFormat.StereoFloat64 => byteLength & ~15U,

            _ => 0,
        };
    }
    public static uint AlignAudioLength(uint byteLength, uint bitdepth, uint channels) {
        return byteLength & ~((bitdepth >> 3) * channels - 1);
    }
}