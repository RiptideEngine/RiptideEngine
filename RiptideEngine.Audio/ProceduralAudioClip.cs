namespace RiptideEngine.Audio;

public delegate void RefillCallback(ProceduralAudioClip clip, uint samplePosition, Span<byte> outputBuffer);

/// <summary>
/// Represent clip objects that provide audio data via <see cref="RefillCallback"/>.
/// </summary>
public sealed unsafe class ProceduralAudioClip : StreamingAudioClip {
    internal RefillCallback RefillCallback { get; private set; }

    public ProceduralAudioClip(uint sampleLength, AudioFormat format, uint frequency, RefillCallback refillCallback) : base(sampleLength, format, frequency) {
        ArgumentNullException.ThrowIfNull(refillCallback, nameof(refillCallback));

        RefillCallback = refillCallback;
    }

    public override void GetRawData(uint samplePosition, Span<byte> outputs) {
        if (RefillCallback == null || samplePosition >= SampleLength || outputs.IsEmpty) return;

        RefillCallback(this, samplePosition, outputs);
    }

    public override uint GetNormalizedData(uint samplePosition, uint channel, Span<float> outputs) {
        if (RefillCallback == null || samplePosition >= SampleLength || outputs.IsEmpty) return 0;
        
        // TODO: Implement this shit.
        throw new NotImplementedException();
    }

    protected override void Dispose() {
        RefillCallback = null!;
    }
}