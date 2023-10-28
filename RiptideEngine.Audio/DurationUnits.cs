namespace RiptideEngine.Audio;

public readonly struct DurationUnits {
    public readonly float Seconds;
    public readonly uint Bytes;
    public readonly uint Samples;
    public readonly uint Frames;

    public DurationUnits(float seconds, uint bytes, uint samples, uint frames) {
        Seconds = seconds;
        Bytes = bytes;
        Samples = samples;
        Frames = frames;
    }

    public static DurationUnits FromBytes(uint bytes, uint bitdepth, uint channels, uint frequency) {
        uint samples = bytes / (bitdepth >> 3);
        uint frames = samples / channels;
        float sec = (float)frames / frequency;

        return new(sec, samples * (bitdepth >> 3), samples, frames);
    }

    public static DurationUnits FromSecond(float sec, uint bitdepth, uint channels, uint frequency) {
        uint frames = (uint)MathF.Floor(sec * frequency);
        uint samples = frames * channels;
        uint bytes = samples * (bitdepth >> 3);

        return new(sec, bytes, samples, frames);
    }

    public static DurationUnits FromSamples(uint samples, uint bitdepth, uint channels, uint frequency) {
        uint frames = samples / channels;
        float sec = (float)frames / frequency;
        uint bytes = samples * (bitdepth >> 3);

        return new(sec, bytes, frames * channels, frames);
    }

    public static DurationUnits FromFrames(uint frames, uint bitdepth, uint channels, uint frequency) {
        uint samples = frames * channels;
        uint bytes = samples * (bitdepth >> 3);
        float sec = (float)frames / frequency;

        return new(sec, bytes, samples, frames);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Seconds, Bytes, Samples, Frames);
    }

    public override string? ToString() {
        return $"<Seconds = {Seconds}, Bytes = {Bytes}, Samples = {Samples}, Frames = {Frames}>";
    }
}
