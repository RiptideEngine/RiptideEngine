namespace RiptideEngine.Audio;

unsafe partial class AudioSource {
    private sealed class StreamingImplementation : Implementation {
        private uint _nextStreamingSample;
        private uint _finishedSample;

        private StreamingBufferArray _buffers;
        private readonly AudioSource _source;

        public override bool IsPlaying {
            get {
                AudioEngine.AL!.GetSourceProperty(_source._source, GetSourceInteger.SourceState, out int state);
                if ((SourceState)state != SourceState.Playing) return false;
                
                AudioEngine.AL.GetSourceProperty(_source._source, GetSourceInteger.SampleOffset, out int offset);
                return _finishedSample + offset < Unsafe.As<StreamingAudioClip>(_source._clip!).SampleLength;
            }
        }
        public override float ElapsedSeconds {
            get {
                AudioEngine.AL!.GetSourceProperty(_source._source, SourceFloat.SecOffset, out float sec);
                return (float)_finishedSample / _source._clip!.Frequency + sec;
            }
        }

        public override uint ElapsedSamples {
            get {
                AudioEngine.AL!.GetSourceProperty(_source._source, GetSourceInteger.SampleOffset, out int sample);
                return _finishedSample + (uint)sample;
            }
        }

        public StreamingImplementation(AudioSource source) {
            _source = source;

            fixed (uint* pBuffers = &MemoryMarshal.GetReference<uint>(_buffers)) {
                AudioEngine.AL!.GenBuffers(AudioEngine.StreamingBufferCount, pBuffers);
            }
        }

        public override void Play() {
            Debug.Assert(_source._clip is StreamingAudioClip);
            
            var al = AudioEngine.AL!;
            
            al.GetSourceProperty(_source._source, GetSourceInteger.SourceState, out int state);

            if ((SourceState)state != SourceState.Paused) {
                var clip = Unsafe.As<StreamingAudioClip>(_source._clip);
                bool cvt = Converting.TryConvert(clip.Format, out var alFormat);
                Debug.Assert(cvt);

                (var bitdepth, var channels) = AudioUtils.GetBitdepthAndNumChannels(clip.Format);

                uint remainFrames = clip.SampleLength - _nextStreamingSample;
                uint samplesPerBuffer = AudioUtils.ByteToSample(AudioEngine.StreamingBufferSize, bitdepth, channels);

                fixed (uint* pBuffers = &MemoryMarshal.GetReference<uint>(_buffers)) {
                    if (remainFrames > samplesPerBuffer * AudioEngine.StreamingBufferCount) {
                        for (int i = 0; i < AudioEngine.StreamingBufferCount; i++) {
                            al.GetBufferProperty(pBuffers[i], GetBufferInteger.Size, out int size);
                            if (size != AudioEngine.StreamingBufferSize) {
                                OALBufferMapping.alBufferStorageSOFT(pBuffers[i], alFormat, null, AudioEngine.StreamingBufferSize, (int)clip.Frequency, BufferMapFlags.Write);
                            }

                            ReadDataIntoBuffer(pBuffers[i], AudioEngine.StreamingBufferSize);

                            _nextStreamingSample += samplesPerBuffer;
                        }

                        al.SourceQueueBuffers(_source._source, AudioEngine.StreamingBufferCount, pBuffers);

                        AudioEngine.OnBufferCompleted += ContinueStreamingCallback;
                    } else {
                        (uint full, uint remain) = uint.DivRem(remainFrames, samplesPerBuffer);
                        Debug.Assert(full < AudioEngine.StreamingBufferCount, "full < AudioEngine.StreamingBufferCount");

                        for (int i = 0; i < full; i++) {
                            al.GetBufferProperty(pBuffers[i], GetBufferInteger.Size, out int size);
                            if (size != AudioEngine.StreamingBufferSize) {
                                OALBufferMapping.alBufferStorageSOFT(pBuffers[i], alFormat, null, AudioEngine.StreamingBufferSize, (int)clip.Frequency, BufferMapFlags.Write);
                            }

                            ReadDataIntoBuffer(pBuffers[i], AudioEngine.StreamingBufferSize);

                            _nextStreamingSample += samplesPerBuffer;
                        }

                        al.SourceQueueBuffers(_source._source, (int)full, pBuffers);

                        if (remain > 0) {
                            int remainBytes = (int)AudioUtils.ByteToSample(remain, bitdepth, channels);

                            OALBufferMapping.alBufferStorageSOFT(pBuffers[full], alFormat, null, remainBytes, (int)clip.Frequency, BufferMapFlags.Write);
                            ReadDataIntoBuffer(pBuffers[full], remainBytes);

                            al.SourceQueueBuffers(_source._source, 1, pBuffers + full);
                        }

                        _nextStreamingSample = 0;
                    }
                }
            }

            al.SourcePlay(_source._source);
        }

        public override void Pause() {
            AudioEngine.AL!.SourcePause(_source._source);
        }

        public override void Stop() {
            var al = AudioEngine.AL!;
            
            al.SourceStop(_source._source);
            
            al.GetSourceProperty(_source._source, GetSourceInteger.BuffersQueued, out int queued);
            while (queued > 0) {
                uint unqueue;
                al.SourceUnqueueBuffers(_source._source, 1, &unqueue);
                queued--;
            }

            AudioEngine.OnBufferCompleted -= ContinueStreamingCallback;
            _nextStreamingSample = _finishedSample = 0;
        }

        public override void SetPlaybackPosition(uint samplePosition) {
            
        }

        public override void Dispose() {
            var al = AudioEngine.AL!;
            
            al.DeleteBuffers(3, (uint*)Unsafe.AsPointer(ref MemoryMarshal.GetReference<uint>(_buffers)));
            _buffers = default;
        }
        
        private void ReadDataIntoBuffer(uint buffer, int size) {
            var ptr = OALBufferMapping.alMapBufferSOFT(buffer, 0, size, BufferMapFlags.Write);
            Unsafe.As<StreamingAudioClip>(_source._clip)!.GetRawData(_nextStreamingSample, new(ptr, size));
            OALBufferMapping.alUnmapBufferSOFT(buffer);
        }
        
        private void ContinueStreamingCallback(uint src, uint numBuffers) {
            if (src != _source._source) return;
            
            var al = AudioEngine.AL!;
            var clip = Unsafe.As<StreamingAudioClip>(_source._clip!);
            
            (var bitdepth, var channels) = AudioUtils.GetBitdepthAndNumChannels(clip.Format);
            uint samplesPerBuffer = AudioUtils.ByteToSample(AudioEngine.StreamingBufferSize, bitdepth, channels);
            
            bool cvt = Converting.TryConvert(clip.Format, out var alFormat);
            Debug.Assert(cvt);
            
            al.GetSourceProperty(src, GetSourceInteger.BuffersProcessed, out int processed);
            
            while (numBuffers > 0) {
                uint buffer;
                al.SourceUnqueueBuffers(src, 1, &buffer);
                
                if (_nextStreamingSample + samplesPerBuffer <= clip.SampleLength) {
                    al.GetBufferProperty(buffer, GetBufferInteger.Size, out int size);
                    if (size != AudioEngine.StreamingBufferSize) {
                        OALBufferMapping.alBufferStorageSOFT(buffer, alFormat, null, AudioEngine.StreamingBufferSize, (int)clip.Frequency, BufferMapFlags.Write);
                    }
                    
                    ReadDataIntoBuffer(buffer, AudioEngine.StreamingBufferSize);
                    
                    _nextStreamingSample += samplesPerBuffer;
                    al.SourceQueueBuffers(src, 1, &buffer);
                } else {
                    int remainBytes = (int)AudioUtils.SampleToByte(clip.SampleLength - _nextStreamingSample, bitdepth, channels);
                    
                    OALBufferMapping.alBufferStorageSOFT(buffer, alFormat, null, remainBytes, (int)clip.Frequency, BufferMapFlags.Write);

                    ReadDataIntoBuffer(buffer, remainBytes);

                    _nextStreamingSample = 0;

                    al.SourceQueueBuffers(src, 1, &buffer);
                    _finishedSample += samplesPerBuffer;

                    AudioEngine.OnBufferCompleted -= ContinueStreamingCallback;
                    AudioEngine.OnBufferCompleted += FinishStreamingCallback;
                    
                    break;
                }
            
                numBuffers--;
                _finishedSample += samplesPerBuffer;
            }
        }

        private void FinishStreamingCallback(uint src, uint numBuffers) {
            if (src != _source._source) return;
            
            var al = AudioEngine.AL!;
            
            var clip = Unsafe.As<StreamingAudioClip>(_source._clip!);
            (var bitdepth, var channels) = AudioUtils.GetBitdepthAndNumChannels(clip.Format);
            
            while (numBuffers > 0) {
                uint buffer;
                al.SourceUnqueueBuffers(src, 1, &buffer);
                
                al.GetBufferProperty(buffer, GetBufferInteger.Size, out int size);
                _finishedSample += AudioUtils.ByteToSample((uint)size, bitdepth, channels);
                
                numBuffers--;
            }

            al.GetSourceProperty(src, GetSourceInteger.BuffersQueued, out int queued);
            if (queued == 0) {
                AudioEngine.OnBufferCompleted -= FinishStreamingCallback;
            }
        }
        
        [InlineArray(AudioEngine.StreamingBufferCount)]
        private struct StreamingBufferArray {
            private uint _buffer;
        }
    }
}