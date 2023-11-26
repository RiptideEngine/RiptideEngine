namespace RiptideEngine.Audio;

unsafe partial class AudioSource {
    /// <summary>
    /// Implementation of audio source to handle the case in which the clip's size is less than the size of all streaming buffers (as defined by <see cref="AudioEngine.StreamingBufferCount"/> and <see cref="AudioEngine.StreamingBufferSize"/>).
    /// </summary>
    /// <param name="source"></param>
    private sealed class InsufficientStreamImplementation(AudioSource source) : Implementation {
        private uint _buffer;

        public override bool IsPlaying {
            get {
                AudioEngine.AL!.GetSourceProperty(source._source, GetSourceInteger.SourceState, out int state);
                return (SourceState)state == SourceState.Playing;
            }
        }

        public override void Play() {
            Debug.Assert(source._clip is StreamingAudioClip);

            var clip = Unsafe.As<StreamingAudioClip>(source._clip);

            var al = AudioEngine.AL!;

            if (_buffer == 0) {
                var byteLength = (int)clip.Durations.Bytes;

                _buffer = al.GenBuffer();

                bool cvt = OALConvert.TryConvert(clip.Format, out var alFormat);
                Debug.Assert(cvt);

                OALBufferMapping.alBufferStorageSOFT(_buffer, alFormat, null, byteLength, (int)clip.Frequency, BufferMapFlags.Write);

                var ptr = OALBufferMapping.alMapBufferSOFT(_buffer, 0, byteLength, BufferMapFlags.Write);
                Debug.Assert(ptr != null);

                clip.GetData(0, new(ptr, byteLength));

                OALBufferMapping.alUnmapBufferSOFT(_buffer);
            }

            al.SetSourceProperty(source._source, SourceInteger.Buffer, _buffer);
            al.SourcePlay(source._source);
        }

        public override void Pause() {
            AudioEngine.AL!.SourcePause(source._source);
        }

        public override void Stop() {
            AudioEngine.AL!.SourceStop(source._source);
        }

        public override void Dispose() {
            AudioEngine.AL!.DeleteBuffer(_buffer); _buffer = 0;
        }
    }

    private sealed class StreamingImplementation(AudioSource source) : Implementation {
        private uint _streamedFrame;
        private uint _continueFrame;
        private uint _bufferPlayedFrame;

        [InlineArray(AudioEngine.StreamingBufferCount)]
        private unsafe struct AudioBufferArray {
            private uint _buffer;
            public readonly ref uint GetPinnableReference() => ref *(uint*)Unsafe.AsPointer(ref Unsafe.AsRef(in this));
        }

        private AudioBufferArray _streamingBuffers;

        public override bool IsPlaying {
            get {
                AudioEngine.AL!.GetSourceProperty(source._source, GetSourceInteger.SourceState, out int state);

                if ((SourceState)state != SourceState.Playing) return false;

                AudioEngine.AL!.GetSourceProperty(source._source, GetSourceInteger.SampleOffset, out int offset);

                Console.WriteLine(_bufferPlayedFrame);
                return _bufferPlayedFrame + offset < Unsafe.As<StreamingAudioClip>(source._clip!).FrameCount;
            }
        }

        public override void Play() {
            Debug.Assert(source._clip is StreamingAudioClip);

            _streamedFrame = _bufferPlayedFrame = _continueFrame;

            var clip = Unsafe.As<StreamingAudioClip>(source._clip);

            bool cvt = OALConvert.TryConvert(clip.Format, out var alFormat);
            Debug.Assert(cvt);
            var al = AudioEngine.AL!;

            if (_streamingBuffers[0] == 0) {
                fixed (uint* pBuffer = _streamingBuffers) {
                    al.GenBuffers(AudioEngine.StreamingBufferCount, pBuffer);

                    for (int i = 0; i < AudioEngine.StreamingBufferCount; i++) {
                        OALBufferMapping.alBufferStorageSOFT(_streamingBuffers[i], alFormat, null, AudioEngine.StreamingBufferSize, (int)clip.Frequency, BufferMapFlags.Write);
                        AudioEngine.AssertNoError();
                    }
                }
            }

            uint numFramesToRead = clip.FrameCount - _streamedFrame;

            Debug.Assert(numFramesToRead > 0);

            (var bitdepth, var channels) = AudioHelper.GetBitdepthAndNumChannels(clip.Format);
            uint numFramesPerBuffer = AudioEngine.StreamingBufferSize / (bitdepth >> 3) / channels;

            Console.WriteLine("Streamed Frame: " + _streamedFrame);

            if (numFramesToRead > numFramesPerBuffer * AudioEngine.StreamingBufferCount) {
                Console.WriteLine("Begin Stream");

                for (int i = 0; i < AudioEngine.StreamingBufferCount; i++) {
                    ReadDataIntoBuffer(_streamingBuffers[i]);

                    _streamedFrame += numFramesPerBuffer;
                }

                fixed (uint* pBuffer = _streamingBuffers) {
                    al.SourceQueueBuffers(source._source, AudioEngine.StreamingBufferCount, pBuffer);
                }

                StreamingCallback.RegisterCallback(source._source, StreamCallback, StopStreamCallback);
            } else {
                (uint full, uint remain) = uint.DivRem(numFramesToRead, numFramesPerBuffer);

                for (int i = 0; i < full; i++) {
                    ReadDataIntoBuffer(_streamingBuffers[i]);

                    _streamedFrame += numFramesPerBuffer;
                }

                if (remain > 0) {
                    var ptr = OALBufferMapping.alMapBufferSOFT(_streamingBuffers[(int)full], 0, AudioEngine.StreamingBufferSize, BufferMapFlags.Write);
                    Debug.Assert(al.GetError() == AudioError.NoError);

                    var byteSize = (int)(remain * (bitdepth >> 3) * channels);
                    Debug.Assert(byteSize < AudioEngine.StreamingBufferSize);

                    clip.GetData(_streamedFrame, new(ptr, byteSize));

                    OALBufferMapping.alUnmapBufferSOFT(_streamingBuffers[(int)full]);
                }

                fixed (uint* pBuffer = _streamingBuffers) {
                    al.SourceQueueBuffers(source._source, (int)full + (remain > 0 ? 1 : 0), pBuffer);
                }

                _streamedFrame = 0;
            }

            al.SourcePlay(source._source);
        }

        private void ReadDataIntoBuffer(uint buffer) {
            var ptr = OALBufferMapping.alMapBufferSOFT(buffer, 0, AudioEngine.StreamingBufferSize, BufferMapFlags.Write);
            AudioEngine.AssertNoError();
            Debug.Assert(ptr != null);

            Unsafe.As<StreamingAudioClip>(source._clip)!.GetData(_streamedFrame, new(ptr, AudioEngine.StreamingBufferSize));

            OALBufferMapping.alUnmapBufferSOFT(buffer);
        }

        private void StreamCallback(int numBuffers) {
            Debug.Assert(numBuffers > 0);

            var al = AudioEngine.AL!;

            var clip = Unsafe.As<StreamingAudioClip>(source._clip!);
            if (_streamedFrame >= clip.FrameCount) {
                while (numBuffers > 0) {
                    uint buffer;
                    al.SourceUnqueueBuffers(source._source, 1, &buffer);

                    numBuffers--;
                }

                al.GetSourceProperty(source._source, GetSourceInteger.BuffersQueued, out int queued);
                if (queued == 0) {
                    _streamedFrame = 0;
                }

                return;
            }

            (var bitdepth, var channels) = AudioHelper.GetBitdepthAndNumChannels(clip.Format);
            uint numFramesPerBuffer = AudioEngine.StreamingBufferSize / (bitdepth >> 3) / channels;

            while (numBuffers > 0) {
                uint buffer;
                al.SourceUnqueueBuffers(source._source, 1, &buffer);

                if (_streamedFrame + numFramesPerBuffer <= clip.FrameCount) {
                    void* ptr = OALBufferMapping.alMapBufferSOFT(buffer, 0, AudioEngine.StreamingBufferSize, BufferMapFlags.Write);

                    clip.GetData(_streamedFrame, new(ptr, AudioEngine.StreamingBufferSize));

                    OALBufferMapping.alUnmapBufferSOFT(buffer);

                    _streamedFrame += numFramesPerBuffer;
                    al.SourceQueueBuffers(source._source, 1, &buffer);

                    numBuffers--;
                } else {
                    int mapLength = (int)((clip.FrameCount - _streamedFrame) * (bitdepth >> 3) * channels);

                    void* ptr = OALBufferMapping.alMapBufferSOFT(buffer, 0, AudioEngine.StreamingBufferSize, BufferMapFlags.Write);

                    var span = new Span<byte>(ptr, AudioEngine.StreamingBufferSize);
                    clip.GetData(_streamedFrame, span[..mapLength]);
                    span[mapLength..].Clear();

                    OALBufferMapping.alUnmapBufferSOFT(buffer);

                    _streamedFrame = clip.FrameCount;
                    al.SourceQueueBuffers(source._source, 1, &buffer);

                    break;
                }

                _bufferPlayedFrame += numFramesPerBuffer;
            }
        }

        private void StopStreamCallback() {
            Console.WriteLine("Stop callback");

            AudioEngine.AL!.GetSourceProperty(source._source, GetSourceInteger.BuffersQueued, out int queued);

            while (queued > 0) {
                uint unqueue;
                AudioEngine.AL!.SourceUnqueueBuffers(source._source, 1, &unqueue);
                queued--;
            }

            AudioEngine.AL!.SourceStop(source._source);

            _continueFrame = _streamedFrame;
            _streamedFrame = _bufferPlayedFrame = 0;
        }

        public override void Pause() {
            AudioEngine.AL!.SourcePause(source._source);
        }

        public override void Stop() {
            AudioEngine.AL!.SourceStop(source._source);
        }

        public override void Dispose() {
            fixed (uint* pBuffer = _streamingBuffers) {
                AudioEngine.AL!.DeleteBuffers(3, pBuffer);
            }

            _streamingBuffers = default;
        }
    }
}