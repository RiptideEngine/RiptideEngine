namespace RiptideEngine.Audio;

public sealed unsafe partial class AudioSource : RiptideObject, IReferenceCount {
    private ulong _refcount;

    private uint _source;

    private AudioClip? _clip;
    private Implementation? _impl;

    public AudioClip? Clip {
        get => _clip;
        set {
            if (ReferenceEquals(_clip, value)) return;

            if (_source != 0) {
                var al = AudioEngine.AL!;

                if (SourceState == SourceState.Playing) {
                    al.SourceStop(_source);
                    al.SetSourceProperty(_source, SourceInteger.Buffer, 0);
                }
            }

            if (_clip != null) {
                Debug.Assert(_impl != null);

                _clip.DecrementReference();
                _impl!.Dispose(); _impl = null;
            }

            _clip?.DecrementReference();
            _clip = value;
            
            if (_clip != null) {
                _clip.IncrementReference();

                if (_clip.IsStreaming) {
                    Debug.Assert(_clip is StreamingAudioClip, "Foreign streaming audio clip detected.");

                    StreamingAudioClip clip = Unsafe.As<StreamingAudioClip>(_clip);

                    (uint bitdepth, uint channels) = AudioHelper.GetBitdepthAndNumChannels(clip.Format);

                    if (clip.FrameCount * (bitdepth >> 3) * channels > AudioEngine.StreamingBufferCount * AudioEngine.StreamingBufferSize) {
                        _impl = new StreamingImplementation(this);
                    } else {
                        _impl = new InsufficientStreamImplementation(this);
                    }
                } else {
                    _impl = new MemoryImplementation(this);
                }

                Console.WriteLine(_impl.GetType().FullName);
            }
        }
    }

    public Vector3 Position {
        get {
            AudioEngine.AL!.GetSourceProperty(_source, SourceVector3.Position, out var pos);
            return pos;
        }
        set {
            AudioEngine.AL!.SetSourceProperty(_source, SourceVector3.Position, value);
        }
    }
    public Vector3 Velocity {
        get {
            AudioEngine.AL!.GetSourceProperty(_source, SourceVector3.Velocity, out var vel);
            return vel;
        }
        set {
            AudioEngine.AL!.SetSourceProperty(_source, SourceVector3.Velocity, value);
        }
    }
    public Vector3 Direction {
        get {
            AudioEngine.AL!.GetSourceProperty(_source, SourceVector3.Direction, out var dir);
            return dir;
        }
        set {
            AudioEngine.AL!.SetSourceProperty(_source, SourceVector3.Direction, value);
        }
    }

    public float Pitch {
        get {
            AudioEngine.AL!.GetSourceProperty(_source, SourceFloat.Pitch, out var pitch);
            return pitch;
        }
        set {
            AudioEngine.AL!.SetSourceProperty(_source, SourceFloat.Pitch, value);
        }
    }
    public float Volume {
        get {
            AudioEngine.AL!.GetSourceProperty(_source, SourceFloat.Gain, out var gain);
            return gain;
        }
        set {
            AudioEngine.AL!.SetSourceProperty(_source, SourceFloat.Gain, value);
        }
    }

    private SourceState SourceState {
        get {
            AudioEngine.AL!.GetSourceProperty(_source, GetSourceInteger.SourceState, out int state);
            return (SourceState)state;
        }
    }

    public bool IsPlaying => _impl?.IsPlaying ?? false;

    public AudioSource() {
        AudioEngine.EnsureInitialized();

        var al = AudioEngine.AL!;
        _source = al.GenSource();
        Debug.Assert(al.GetError() == AudioError.NoError);

        al.SetSourceProperty(_source, SourceFloat.Gain, 0.5f);

        _refcount = 1;
    }

    public void Play() {
        if (_source == 0 || _clip == null || _clip.GetReferenceCount() == 0) return;
        if (_impl!.IsPlaying) return;

        _impl!.Play();
    }

    public void Pause() {
        if (_source == 0 || _impl == null) return;

        _impl.Pause();
    }

    public void Stop() {
        if (_source == 0 || _impl == null) return;

        _impl.Stop();
    }

    public ulong IncrementReference() {
        return _refcount == 0 ? 0 : ++_refcount;
    }

    public ulong DecrementReference() {
        switch (_refcount) {
            case 0: return 0;
            case 1:
                Dispose();
                _refcount = 0;
                return 0;
            default: return --_refcount;
        }
    }

    public ulong GetReferenceCount() => _refcount;

    private void Dispose() {
        _impl?.Stop();

        if (_clip != null) {
            _clip.DecrementReference(); _clip = null;
            _impl!.Dispose(); _impl = null;
        }
    }

    private abstract class Implementation : IDisposable {
        public abstract bool IsPlaying { get; }

        public abstract void Play();
        public abstract void Pause();
        public abstract void Stop();

        public virtual void Dispose() { }
    }
}