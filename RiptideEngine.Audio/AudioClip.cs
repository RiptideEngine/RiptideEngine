namespace RiptideEngine.Audio;

public abstract class AudioClip : AudioObject {
    public abstract uint SampleLength { get; }
    public abstract uint ByteLength { get; }
    public abstract float Length { get; }

    public abstract uint Frequency { get; }
    public abstract AudioFormat Format { get; }

    /// <summary>
    /// Read the raw audio data.
    /// </summary>
    /// <param name="samplePosition">Frame position to read from.</param>
    /// <param name="outputs">Output span.</param>
    public abstract void GetRawData(uint samplePosition, Span<byte> outputs);

    /// <summary>
    /// Read the audio data from specific channel as normalized values mapped to range -1 to 1.
    /// </summary>
    /// <param name="samplePosition">Frame position to read from.</param>
    /// <param name="channel">Channel to read from. Use value larger than format's channel count will read datas from all channels (data will be interleaved).</param>
    /// <param name="outputs">Output span.</param>
    /// <returns>Amount of samples read.</returns>
    public abstract uint GetNormalizedData(uint samplePosition, uint channel, Span<float> outputs);
}