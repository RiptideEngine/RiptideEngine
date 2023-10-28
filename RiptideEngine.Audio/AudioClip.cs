namespace RiptideEngine.Audio;

public abstract class AudioClip : RiptideObject, IReferenceCount {
    public abstract DurationUnits Durations { get; }

    public abstract uint Frequency { get; }
    public abstract AudioFormat Format { get; }

    /// <summary>
    /// Read the raw audio data.
    /// </summary>
    /// <param name="framePosition">Frame position to read at.</param>
    /// <param name="outputBuffer">Span to receive output data.</param>
    public abstract void GetData(uint framePosition, Span<byte> outputBuffer);

    protected ulong _refcount = 1;

    public abstract bool IsStreaming { get; }

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

    protected abstract void Dispose();
}