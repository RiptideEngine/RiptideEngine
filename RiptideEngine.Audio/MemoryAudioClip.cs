using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RiptideEngine.Audio;

/// <summary>
/// Represent clip objects that hold all audio data in memory.
/// </summary>
public sealed unsafe class MemoryAudioClip : AudioClip {
    private uint _buffer;
    private byte* pMapped;

    public uint Buffer => _buffer;

    public override float Length {
        get {
            if (_buffer == 0) return 0;
            
            AudioEngine.AL!.GetBufferProperty(_buffer, (BufferFloat)0x200B, out float output);
            return output;
        }
    }

    public override uint ByteLength {
        get {
            if (_buffer == 0) return 0;
            
            AudioEngine.AL!.GetBufferProperty(_buffer, (GetBufferInteger)0x2009, out int output);
            return (uint)output;
        }
    }

    public override uint SampleLength {
        get {
            if (_buffer == 0) return 0;
            
            AudioEngine.AL!.GetBufferProperty(_buffer, (GetBufferInteger)0x200A, out int output);
            return (uint)output;
        }
    }

    public override uint Frequency {
        get {
            AudioEngine.AL!.GetBufferProperty(_buffer, GetBufferInteger.Frequency, out int frequency);
            return (uint)frequency;
        }
    }

    public override AudioFormat Format { get; }

    public MemoryAudioClip(uint sampleLength, AudioFormat format, uint frequency, ReadOnlySpan<byte> data) {
        AudioEngine.EnsureInitialized();
        if (!AudioCapability.IsFormatSupported(format)) throw new PlatformNotSupportedException(string.Format(ExceptionMessages.UnsupportedAudioFormat, format));

        var al = AudioEngine.AL!;

        _buffer = al.GenBuffer();

        bool convert = Converting.TryConvert(format, out var alFormat);
        Debug.Assert(convert);

        (uint bitdepth, uint channels) = AudioUtils.GetBitdepthAndNumChannels(format);
        uint bufferLength = AudioUtils.SampleToByte(sampleLength, bitdepth, channels);

        OALBufferMapping.alBufferStorageSOFT(_buffer, alFormat, null, (int)bufferLength, (int)frequency, BufferMapFlags.Read | BufferMapFlags.Write | BufferMapFlags.Persistent);

        AudioError error = al.GetError();
        switch (error) {
            case AudioError.OutOfMemory: throw new OutOfMemoryException("OpenAL failed to allocate memory for audio buffer storage.");
            case AudioError.InvalidValue: throw new NotSupportedException($"OpenAL audio format '{alFormat}' is not supported or invalid.");
        }

        pMapped = (byte*)OALBufferMapping.alMapBufferSOFT(_buffer, 0, (int)bufferLength, BufferMapFlags.Read | BufferMapFlags.Write | BufferMapFlags.Persistent);

        if (!data.IsEmpty) {
            Unsafe.CopyBlock(pMapped, Unsafe.AsPointer(ref MemoryMarshal.GetReference(data)), uint.Min((uint)data.Length, bufferLength));
        }

        Format = format;
        _refcount = 1;
    }

    public MemoryAudioClip(uint sampleLength, AudioFormat format, uint frequency, Stream stream) {
        AudioEngine.EnsureInitialized();
        if (!AudioCapability.IsFormatSupported(format)) throw new PlatformNotSupportedException(string.Format(ExceptionMessages.UnsupportedAudioFormat, format));
        if (!stream.CanRead) throw new ArgumentException(ExceptionMessages.FailedToCreate_UnreadableStream);

        var al = AudioEngine.AL!;

        _buffer = al.GenBuffer();

        (uint bitdepth, uint channels) = AudioUtils.GetBitdepthAndNumChannels(format);
        uint bufferLength = AudioUtils.SampleToByte(sampleLength, bitdepth, channels);
        
        bool convert = Converting.TryConvert(format, out var alFormat);
        Debug.Assert(convert);
        
        OALBufferMapping.alBufferStorageSOFT(_buffer, alFormat, null, (int)bufferLength, (int)frequency, BufferMapFlags.Write | BufferMapFlags.Persistent);

        AudioError error = al.GetError();
        switch (error) {
            case AudioError.OutOfMemory: throw new OutOfMemoryException("OpenAL failed to allocate memory for audio buffer storage.");
            case AudioError.InvalidValue: throw new NotSupportedException($"OpenAL audio format '{alFormat}' is not supported or invalid.");
        }

        pMapped = (byte*)OALBufferMapping.alMapBufferSOFT(_buffer, 0, (int)bufferLength, BufferMapFlags.Write | BufferMapFlags.Persistent);

        stream.ReadExactly(new(pMapped, (int)bufferLength));

        Format = format;
        _refcount = 1;
    }

    public override void GetRawData(uint samplePosition, Span<byte> outputs) {
        if (_buffer == 0 || samplePosition >= SampleLength || outputs.IsEmpty) return;

        (uint bitdepth, uint channels) = AudioUtils.GetBitdepthAndNumChannels(Format);
        
        uint copyLength = uint.Min(AudioUtils.AlignByte((uint)outputs.Length, bitdepth, channels), AudioUtils.SampleToByte(SampleLength - samplePosition, bitdepth, channels));
        Debug.Assert(copyLength != 0, "copyLength != 0");

        fixed (byte* pOutput = outputs) {
            uint bytePosition = AudioUtils.SampleToByte(samplePosition, bitdepth, channels);
            
            Unsafe.CopyBlock(pOutput,  pMapped + bytePosition, copyLength);
        }
    }

    public override uint GetNormalizedData(uint samplePosition, uint channel, Span<float> outputs) {
        if (_buffer == 0 || samplePosition >= SampleLength || outputs.IsEmpty) return 0;

        uint amount = uint.Min((uint)outputs.Length, SampleLength - samplePosition);

        fixed (float* pOutput = outputs) {
            switch (Format) {
                case AudioFormat.Mono8: {
                    if (BitConverter.IsLittleEndian) {
                        if (Avx2.IsSupported) {
                            uint i = 0;
                            uint alignedAmount = amount - amount % (uint)Vector256<byte>.Count;

                            Vector256<float> coefficient = Vector256.Create(0.007843137254902f);
                            
                            while (i < alignedAmount) {
                                Vector256<byte> load = Avx.LoadVector256(pMapped + samplePosition + i);
                                
                                Vector256<short> up0 = Avx2.UnpackLow(load, Vector256<byte>.Zero).AsInt16();
                                Vector256<short> up1 = Avx2.UnpackHigh(load, Vector256<byte>.Zero).AsInt16();
                                
                                Vector256<int> up00 = Avx2.UnpackLow(up0, Vector256<short>.Zero).AsInt32();
                                Vector256<int> up01 = Avx2.UnpackHigh(up0, Vector256<short>.Zero).AsInt32();
                                Vector256<int> up10 = Avx2.UnpackLow(up1, Vector256<short>.Zero).AsInt32();
                                Vector256<int> up11 = Avx2.UnpackHigh(up1, Vector256<short>.Zero).AsInt32();

                                Vector256<float> v1, v2, v3, v4;

                                if (Fma.IsSupported) {
                                    v1 = Fma.MultiplySubtract(Avx.ConvertToVector256Single(up00), coefficient, Vector256<float>.One);
                                    v2 = Fma.MultiplySubtract(Avx.ConvertToVector256Single(up01), coefficient, Vector256<float>.One);
                                    v3 = Fma.MultiplySubtract(Avx.ConvertToVector256Single(up10), coefficient, Vector256<float>.One);
                                    v4 = Fma.MultiplySubtract(Avx.ConvertToVector256Single(up11), coefficient, Vector256<float>.One);
                                } else {
                                    v1 = Avx.Subtract(Avx.Multiply(Avx.ConvertToVector256Single(up00), coefficient), Vector256<float>.One);
                                    v2 = Avx.Subtract(Avx.Multiply(Avx.ConvertToVector256Single(up01), coefficient), Vector256<float>.One);
                                    v3 = Avx.Subtract(Avx.Multiply(Avx.ConvertToVector256Single(up10), coefficient), Vector256<float>.One);
                                    v4 = Avx.Subtract(Avx.Multiply(Avx.ConvertToVector256Single(up11), coefficient), Vector256<float>.One);
                                }
                                
                                Avx.Store(pOutput + i, v1);
                                Avx.Store(pOutput + i + 8, v2);
                                Avx.Store(pOutput + i + 16, v3);
                                Avx.Store(pOutput + i + 24, v4);
                                
                                i += (uint)Vector256<byte>.Count;
                            }
                        
                            for (; i < amount; i++) {
                                pOutput[i] = SampleConvert.ToSingle(pMapped[samplePosition + i]);
                            }
                            break;
                        }
                        
                        if (Sse2.IsSupported) {
                            uint i = 0;
                            uint alignedAmount = amount - amount % (uint)Vector128<byte>.Count;
                            
                            Vector128<float> coefficient = Vector128.Create(0.007843137254902f);

                            while (i < alignedAmount) {
                                Vector128<byte> load = Sse2.LoadVector128(pMapped + samplePosition + i);
                                
                                Vector128<short> up0 = Sse2.UnpackLow(load, Vector128<byte>.Zero).AsInt16();
                                Vector128<short> up1 = Sse2.UnpackHigh(load, Vector128<byte>.Zero).AsInt16();

                                Vector128<int> up00 = Sse2.UnpackLow(up0, Vector128<short>.Zero).AsInt32();
                                Vector128<int> up01 = Sse2.UnpackHigh(up0, Vector128<short>.Zero).AsInt32();
                                Vector128<int> up10 = Sse2.UnpackLow(up1, Vector128<short>.Zero).AsInt32();
                                Vector128<int> up11 = Sse2.UnpackHigh(up1, Vector128<short>.Zero).AsInt32();

                                Vector128<float> v1, v2, v3, v4;

                                if (Fma.IsSupported) {
                                    v1 = Fma.MultiplySubtract(Sse2.ConvertToVector128Single(up00), coefficient, Vector128<float>.One);
                                    v2 = Fma.MultiplySubtract(Sse2.ConvertToVector128Single(up01), coefficient, Vector128<float>.One);
                                    v3 = Fma.MultiplySubtract(Sse2.ConvertToVector128Single(up10), coefficient, Vector128<float>.One);
                                    v4 = Fma.MultiplySubtract(Sse2.ConvertToVector128Single(up11), coefficient, Vector128<float>.One);
                                } else {
                                    v1 = Sse.Subtract(Sse.Multiply(Sse2.ConvertToVector128Single(up00), coefficient), Vector128<float>.One);
                                    v2 = Sse.Subtract(Sse.Multiply(Sse2.ConvertToVector128Single(up01), coefficient), Vector128<float>.One);
                                    v3 = Sse.Subtract(Sse.Multiply(Sse2.ConvertToVector128Single(up10), coefficient), Vector128<float>.One);
                                    v4 = Sse.Subtract(Sse.Multiply(Sse2.ConvertToVector128Single(up11), coefficient), Vector128<float>.One);
                                }
                                
                                Sse.Store(pOutput + i, v1);
                                Sse.Store(pOutput + i + 4, v2);
                                Sse.Store(pOutput + i + 8, v3);
                                Sse.Store(pOutput + i + 12, v4);
                                
                                i += (uint)Vector128<byte>.Count;
                            }

                            for (; i < amount; i++) {
                                pOutput[i] = SampleConvert.ToSingle(pMapped[samplePosition + i]);
                            }
                            break;
                        }
                    }

                    for (uint i = 0; i < amount; i++) {
                        pOutput[i] = SampleConvert.ToSingle(pMapped[samplePosition + i]);
                    }
                    break;
                }

                case AudioFormat.Stereo8: {
                    if (channel >= 2) goto case AudioFormat.Mono8;
                    
                    for (uint i = 0; i < amount; i++) {
                        pOutput[i] = SampleConvert.ToSingle(pMapped[samplePosition + i * 2 + channel]);
                    }
                    break;
                }

                case AudioFormat.Mono16: {
                    for (uint i = 0; i < amount; i++) {
                        pOutput[i] = SampleConvert.ToSingle(((short*)pMapped)[samplePosition + i]);
                    }
                    break;
                }

                case AudioFormat.Stereo16: {
                    if (channel >= 2) goto case AudioFormat.Mono16;

                    if (Sse2.IsSupported && BitConverter.IsLittleEndian) {
                        uint i = 0;
                        uint alignedAmount = amount - amount % (uint)Vector128<short>.Count;
                    
                        while (i < alignedAmount) {
                            Vector128<short> load = Sse2.LoadVector128((short*)pMapped + samplePosition + i * 2 + channel);
                    
                            Vector128<float> nlo = Sse.Multiply(Sse2.ConvertToVector128Single(Sse2.UnpackLow(load, Vector128<short>.Zero).AsInt32()), Vector128.Create(0.000030517578125f));
                            Vector128<float> nhi = Sse.Multiply(Sse2.ConvertToVector128Single(Sse2.UnpackHigh(load, Vector128<short>.Zero).AsInt32()), Vector128.Create(0.000030517578125f));
                            
                            Vector128<float> shuffle = Sse.Shuffle(nlo, nhi, 0b10_00_10_00);
                            
                            Sse.Store(pOutput + i, shuffle);
                            
                            i += (uint)Vector128<short>.Count;
                        }
                    
                        for (; i < amount; i++) {
                            pOutput[i] = SampleConvert.ToSingle(((short*)pMapped)[samplePosition + i * 2 + channel]);
                        }
                    } else {
                        for (uint i = 0; i < amount; i++) {
                            pOutput[i] = SampleConvert.ToSingle(((short*)pMapped)[samplePosition + i * 2 + channel]);
                        }
                    }
                    break;
                }

                case AudioFormat.MonoFloat32: {
                    Unsafe.CopyBlock(pOutput, (float*)pMapped + samplePosition, amount * 4);
                    break;
                }

                case AudioFormat.StereoFloat32: {
                    if (channel >= 2) goto case AudioFormat.MonoFloat32;
                    
                    for (uint i = 0; i < amount; i++) {
                        pOutput[i] = ((float*)pMapped)[samplePosition + i * 2 + channel];
                    }
                    break;
                }
                
                default: throw new NotImplementedException($"Unimplemented case '{Format}'.");
            }
        }
        
        return amount;
    }

    protected override void Dispose() {
        AudioEngine.AL!.DeleteBuffer(_buffer); _buffer = 0; pMapped = null;
    }
}