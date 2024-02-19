namespace RiptideEngine.Audio;

partial class AudioSource {
    private sealed class MemoryImplementation(AudioSource source) : Implementation {
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
            Debug.Assert(source._clip is MemoryAudioClip);

            uint buffer = Unsafe.As<MemoryAudioClip>(source._clip)!.Buffer;

            var al = AudioEngine.AL!;

            al.SetSourceProperty(source._source, SourceInteger.Buffer, buffer);
            al.SourcePlay(source._source);
        }

        public override void Pause() {
            AudioEngine.AL!.SourcePause(source._source);
        }

        public override void Stop() {
            AudioEngine.AL!.SourceStop(source._source);
        }
    }
}