using System.Buffers;

namespace RiptideEngine.Audio;

/// <summary>
/// Represent clip objects that provide audio data via a <see cref="StreamReader"/>.
/// </summary>
public sealed class StreamAudioClip : StreamingAudioClip {
    private Stream _stream;
    private readonly bool _ownStream;

    public StreamAudioClip(uint sampleLength, AudioFormat format, uint frequency, Stream stream, bool ownStream = true) : base(sampleLength, format, frequency) {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));
        if (!stream.CanSeek) throw new ArgumentException("Stream must be seekable.");
        if (!stream.CanRead) throw new ArgumentException("Stream must be readable.");

        _stream = stream;
        _ownStream = ownStream;
    }

    public override void GetRawData(uint samplePosition, Span<byte> outputs) {
        if (_stream == null || samplePosition >= SampleLength || outputs.IsEmpty) return;
        
        (uint bitdepth, uint channels) = AudioUtils.GetBitdepthAndNumChannels(Format);
        uint readAmount = uint.Min(AudioUtils.SampleToByte(SampleLength - samplePosition, bitdepth, channels), AudioUtils.AlignByte((uint)outputs.Length, bitdepth, channels));

        lock (_stream) {
            _stream.Seek((int)AudioUtils.SampleToByte(samplePosition, bitdepth, channels), SeekOrigin.Begin);
            _stream.ReadExactly(outputs[..(int)readAmount]);
        }
    }

    public override uint GetNormalizedData(uint samplePosition, uint channel, Span<float> outputs) {
        if (_stream == null || samplePosition >= SampleLength || outputs.IsEmpty) return 0;

        throw new NotImplementedException();

        // (var bitdepth, var channels) = AudioUtils.GetBitdepthAndNumChannels(Format);
        // lock (_stream) {
        //     _stream.Seek((int)AudioUtils.SampleToByte(samplePosition, bitdepth, channels), SeekOrigin.Begin);
        //
        //     switch (Format) {
        //         case AudioFormat.Mono8: {
        //             var reinterpret = MemoryMarshal.AsBytes(outputs);
        //             _stream.ReadExactly(reinterpret[..outputs.Length]);
        //
        //             for (int i = outputs.Length - 1; i >= 0; i--) {
        //                 outputs[i] = SampleConvert.ToSingle(reinterpret[i]);
        //             }
        //             break;
        //         }
        //
        //         case AudioFormat.Stereo8: {
        //             if (channel >= channels) goto case AudioFormat.Mono8;
        //             
        //             var reinterpret = MemoryMarshal.AsBytes(outputs);
        //             _stream.ReadExactly(reinterpret[..(outputs.Length * 2)]);
        //
        //             for (int i = outputs.Length * 2 - 1; i >= 0; i -= 2) {
        //                 outputs[i / 2] = SampleConvert.ToSingle(reinterpret[i - (1 - (int)channel)]);
        //             }
        //             break;
        //         }
        //
        //         case AudioFormat.Mono16: {
        //             var reinterpret = MemoryMarshal.Cast<float, short>(outputs);
        //             _stream.ReadExactly(MemoryMarshal.AsBytes(reinterpret[..(outputs.Length * 2)]));
        //
        //             for (int i = outputs.Length - 1; i >= 0; i--) {
        //                 outputs[i] = SampleConvert.ToSingle(reinterpret[i]);
        //             }
        //             break;
        //         }
        //
        //         case AudioFormat.Stereo16: {
        //             if (channel >= channels) goto case AudioFormat.Mono16;
        //             
        //             var reinterpret = MemoryMarshal.Cast<float, short>(outputs);
        //             _stream.ReadExactly(MemoryMarshal.AsBytes(reinterpret[..(outputs.Length * 2)]));
        //
        //             for (int i = outputs.Length - 1; i >= 0; i -= 2) {
        //                 outputs[i / 2] = SampleConvert.ToSingle(reinterpret[i - (1 - (int)channel)]);
        //             }
        //             break;
        //         }
        //
        //         case AudioFormat.MonoFloat32: {
        //             int _ = _stream.Read(MemoryMarshal.AsBytes(outputs));
        //             break;
        //         }
        //
        //         case AudioFormat.StereoFloat32: {
        //             if (channel >= channels) goto case AudioFormat.MonoFloat32;
        //
        //             int read = _stream.Read(MemoryMarshal.AsBytes(outputs));
        //             
        //             // int _ = _stream.Read(MemoryMarshal.AsBytes(outputs));
        //             // var borrow = ArrayPool<float>.Shared.Rent(outputs.Length);
        //             //
        //             // ArrayPool<float>.Shared.Return(borrow);
        //             break;
        //         }
        //     }
        // }
    }

    protected override void Dispose() {
        if (_ownStream) _stream.Dispose();
        _stream = null!;
    }
}
