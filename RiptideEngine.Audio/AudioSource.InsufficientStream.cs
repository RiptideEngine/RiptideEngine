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

        public override float ElapsedSeconds {
            get {
                AudioEngine.AL!.GetSourceProperty(source._source, SourceFloat.SecOffset, out float sec);
                return sec;
            }
        }

        public override uint ElapsedSamples {
            get {
                AudioEngine.AL!.GetSourceProperty(source._source, GetSourceInteger.SampleOffset, out int sample);
                return (uint)sample;
            }
        }
        
        public override void Play() {
            Debug.Assert(source._clip is StreamingAudioClip);

            var clip = Unsafe.As<StreamingAudioClip>(source._clip);

            var al = AudioEngine.AL!;

            if (_buffer == 0) {
                var byteLength = (int)clip.ByteLength;

                _buffer = al.GenBuffer();

                bool cvt = Converting.TryConvert(clip.Format, out var alFormat);
                Debug.Assert(cvt);

                OALBufferMapping.alBufferStorageSOFT(_buffer, alFormat, null, byteLength, (int)clip.Frequency, BufferMapFlags.Write);

                var ptr = OALBufferMapping.alMapBufferSOFT(_buffer, 0, byteLength, BufferMapFlags.Write);
                Debug.Assert(ptr != null);

                clip.GetRawData(0, new(ptr, byteLength));

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
}