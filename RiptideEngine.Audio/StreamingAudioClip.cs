namespace RiptideEngine.Audio;

public abstract class StreamingAudioClip : AudioClip {
    public sealed override uint Frequency { get; }
    public sealed override AudioFormat Format { get; }
    public sealed override uint SampleLength { get; }

    public sealed override float Length => (float)SampleLength / Frequency;

    public sealed override uint ByteLength => Format switch {
        AudioFormat.Mono8 => SampleLength,
        AudioFormat.Stereo8 or AudioFormat.Mono16 => SampleLength * 2,
        AudioFormat.Stereo16 or AudioFormat.MonoFloat32 => SampleLength * 4,
        AudioFormat.StereoFloat32 => SampleLength * 8,
        _ => 0,
    };

    protected StreamingAudioClip(uint sampleLength, AudioFormat format, uint frequency) {
        ArgumentOutOfRangeException.ThrowIfZero(frequency, nameof(frequency));
        if (!format.IsDefined()) throw new ArgumentException("Undefined audio format provided.", nameof(format));
        
        Frequency = frequency;
        Format = format;
        SampleLength = sampleLength;
    }
}