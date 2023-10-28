namespace RiptideEngine.Audio;

public sealed unsafe class MemoryAudioClip : AudioClip {
    private uint _buffer;
    private byte* pBufferMapped;

    public override bool IsStreaming => false;
    public uint Buffer => _buffer;

    public override DurationUnits Durations {
        get {
            var al = AudioEngine.AL!;

            al.GetBufferProperty(_buffer, (GetBufferInteger)0x2009, out int byteLength);
            al.GetBufferProperty(_buffer, (GetBufferInteger)0x200A, out int sampleLength);
            al.GetBufferProperty(_buffer, (BufferFloat)0x200B, out float secLength);
            al.GetBufferProperty(_buffer, GetBufferInteger.Channels, out int channels);

            return new(secLength, (uint)byteLength, (uint)(sampleLength * channels), (uint)sampleLength);
        }
    }

    public override uint Frequency {
        get {
            AudioEngine.AL!.GetBufferProperty(_buffer, GetBufferInteger.Frequency, out int frequency);
            return (uint)frequency;
        }
    }

    public override AudioFormat Format { get; }

    public MemoryAudioClip(uint frameCount, AudioFormat format, uint frequency, ReadOnlySpan<byte> data) {
        AudioEngine.EnsureInitialized();
        if (!AudioCapability.IsFormatSupported(format)) throw new PlatformNotSupportedException(string.Format(ExceptionMessages.UnsupportedAudioFormat, format));

        var al = AudioEngine.AL!;

        _buffer = al.GenBuffer();

        bool convert = OALConvert.TryConvert(format, out var alFormat);
        Debug.Assert(convert);

        (uint bitdepth, uint channels) = AudioHelper.GetBitdepthAndNumChannels(format);
        uint bufferLength = DurationUnits.FromFrames(frameCount, bitdepth, channels, frequency).Bytes;

        OALBufferMapping.alBufferStorageSOFT(_buffer, alFormat, null, (int)bufferLength, (int)frequency, BufferMapFlags.Read | BufferMapFlags.Write | BufferMapFlags.Persistent);

        AudioError error = al.GetError();
        switch (error) {
            case AudioError.OutOfMemory: throw new OutOfMemoryException("OpenAL failed to allocate memory for audio buffer storage.");
            case AudioError.InvalidValue: throw new NotSupportedException($"OpenAL audio format '{alFormat}' is not supported or invalid.");
        }

        pBufferMapped = (byte*)OALBufferMapping.alMapBufferSOFT(_buffer, 0, (int)bufferLength, BufferMapFlags.Read | BufferMapFlags.Write | BufferMapFlags.Persistent);

        if (!data.IsEmpty) {
            Unsafe.CopyBlock(pBufferMapped, Unsafe.AsPointer(ref MemoryMarshal.GetReference(data)), uint.Min((uint)data.Length, bufferLength));
        }

        Format = format;
        _refcount = 1;
    }

    public MemoryAudioClip(AudioFormat format, uint frequency, Stream stream) {
        AudioEngine.EnsureInitialized();
        if (!AudioCapability.IsFormatSupported(format)) throw new PlatformNotSupportedException(string.Format(ExceptionMessages.UnsupportedAudioFormat, format));
        if (!stream.CanRead) throw new ArgumentException(ExceptionMessages.FailedToCreate_UnreadableStream);

        var al = AudioEngine.AL!;

        _buffer = al.GenBuffer();

        bool convert = OALConvert.TryConvert(format, out var alFormat);
        Debug.Assert(convert);

        uint bufferLength = AudioHelper.AlignAudioLength((uint)(stream.Length - stream.Position), format);

        if (bufferLength == 0) throw new ArgumentException("Stream doesn't contain enough data to read at least 1 frame of audio.");

        OALBufferMapping.alBufferStorageSOFT(_buffer, alFormat, null, (int)bufferLength, (int)frequency, BufferMapFlags.Read | BufferMapFlags.Write | BufferMapFlags.Persistent);

        AudioError error = al.GetError();
        switch (error) {
            case AudioError.OutOfMemory: throw new OutOfMemoryException("OpenAL failed to allocate memory for audio buffer storage.");
            case AudioError.InvalidValue: throw new NotSupportedException($"OpenAL audio format '{alFormat}' is not supported or invalid.");
        }

        pBufferMapped = (byte*)OALBufferMapping.alMapBufferSOFT(_buffer, 0, (int)bufferLength, BufferMapFlags.Read | BufferMapFlags.Write | BufferMapFlags.Persistent);

        stream.Read(new(pBufferMapped, (int)bufferLength));

        _refcount = 1;
    }

    public override void GetData(uint framePosition, Span<byte> outputBuffer) {
        if (_buffer == 0) return;

        var al = AudioEngine.AL!;

        al.GetBufferProperty(_buffer, (GetBufferInteger)0x200A, out int frameLength);

        if (framePosition >= frameLength) return;

        al.GetBufferProperty(_buffer, GetBufferInteger.Bits, out int bitdepth);
        al.GetBufferProperty(_buffer, GetBufferInteger.Channels, out int channels);

        uint cpyLength = AudioHelper.AlignAudioLength((uint)outputBuffer.Length, (uint)bitdepth, (uint)channels);
        if (cpyLength == 0) return;

        uint bytePosition = framePosition * (uint)(bitdepth >> 3) * (uint)channels;

        Unsafe.CopyBlock(ref MemoryMarshal.GetReference(outputBuffer), ref *(pBufferMapped + bytePosition), cpyLength);
    }

    protected override void Dispose() {
        AudioEngine.AL!.DeleteBuffer(_buffer); _buffer = 0; pBufferMapped = null;
    }
}