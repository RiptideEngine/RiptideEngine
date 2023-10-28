namespace RiptideEngine.Audio;

public abstract class StreamingAudioClip(uint frameCount, AudioFormat format, uint frequency) : AudioClip {
    public sealed override bool IsStreaming => true;

    public sealed override uint Frequency { get; } = frequency;
    public sealed override AudioFormat Format { get; } = format;
    
    public readonly uint FrameCount = frameCount;

    public sealed override DurationUnits Durations {
        get {
            if (_refcount == 0) return default;

            (uint bitdepth, uint channels) = AudioHelper.GetBitdepthAndNumChannels(Format);
            return DurationUnits.FromFrames(FrameCount, bitdepth, channels, Frequency);
        }
    }
}