//namespace RiptideEngine.Audio;

//public delegate uint StreamingAudioRefillCallback(DelegateStreamingAudioClip clip, DurationUnits position, Span<byte> outputBuffer);

//public sealed unsafe class DelegateStreamingAudioClip : StreamingAudioClip {
//    internal StreamingAudioRefillCallback RefillCallback { get; private set; }

//    public DelegateStreamingAudioClip(uint frameCount, AudioFormat format, uint frequency, StreamingAudioRefillCallback refillCallback) : base(frameCount, format, frequency) {
//        ArgumentNullException.ThrowIfNull(refillCallback, nameof(refillCallback));

//        RefillCallback = refillCallback;
//    }

//    public override uint GetData(uint framePosition, Span<byte> outputBuffer) {
//        if (framePosition >= _frameCount) return 0;

//        (uint bitdepth, uint channels) = AudioHelper.GetBitdepthAndNumChannels(_format);

//        return RefillCallback(this, DurationUnits.FromFrames(framePosition, bitdepth, channels, _frequency), outputBuffer);
//    }

//    protected override void Dispose() {
//        RefillCallback = null!;
//    }
//}