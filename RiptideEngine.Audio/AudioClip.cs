namespace RiptideEngine.Audio;

public abstract class AudioClip : RiptideRcObject {
    public abstract DurationUnits Durations { get; }

    public abstract uint Frequency { get; }
    public abstract AudioFormat Format { get; }

    /// <summary>
    /// Read the raw audio data.
    /// </summary>
    /// <param name="framePosition">Frame position to read at.</param>
    /// <param name="outputBuffer">Span to receive output data.</param>
    public abstract void GetData(uint framePosition, Span<byte> outputBuffer);

    public abstract bool IsStreaming { get; }
}