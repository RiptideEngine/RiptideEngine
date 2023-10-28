namespace RiptideEngine.Audio;

public sealed unsafe class StreamAudioClip : StreamingAudioClip {
    private Stream _stream;
    private bool _ownStream;

    public StreamAudioClip(uint frameCount, AudioFormat format, uint frequency, Stream stream) : this(frameCount, format, frequency, stream, true) { }

    public StreamAudioClip(uint frameCount, AudioFormat format, uint frequency, Stream stream, bool ownStream) : base(frameCount, format, frequency) {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));
        if (!stream.CanSeek) throw new ArgumentException("Stream must be seekable.");
        if (!stream.CanRead) throw new ArgumentException("Stream must be readable.");

        _stream = stream;
        _ownStream = ownStream;
    }

    public override void GetData(uint framePosition, Span<byte> outputBuffer) {
        if (_stream == null) return;
        if (framePosition >= FrameCount) return;

        (uint bitdepth, uint channels) = AudioHelper.GetBitdepthAndNumChannels(Format);

        uint readAmount = AudioHelper.AlignAudioLength((uint)long.Min(outputBuffer.Length, (FrameCount - framePosition) * (bitdepth >> 3) * channels), bitdepth, channels);
        if (readAmount == 0) return;

        _stream.Seek(framePosition * (bitdepth >> 3) * channels, SeekOrigin.Begin);
        _stream.ReadExactly(outputBuffer[..(int)readAmount]);
    }

    protected override void Dispose() {
        if (_ownStream) _stream.Dispose();
        _stream = null!;
    }
}
