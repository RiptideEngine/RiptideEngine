namespace RiptideEngine.Audio;

[EnumExtension]
public enum AudioFormat {
    /// <summary>
    /// Unsigned 8-bit mono buffer format.
    /// </summary>
    Mono8,

    /// <summary>
    /// Unsigned 8-bit stereo buffer format.
    /// </summary>
    Stereo8,

    /// <summary>
    /// Signed 16-bit mono buffer format.
    /// </summary>
    Mono16,

    /// <summary>
    /// Signed 16-bit stereo buffer format.
    /// </summary>
    Stereo16,

    /// <summary>
    /// 32-bit floating point mono buffer format.
    /// </summary>
    MonoFloat32,

    /// <summary>
    /// 32-bit floating point stereo buffer format.
    /// </summary>
    StereoFloat32,

    /// <summary>
    /// 64-bit floating point mono buffer format.
    /// </summary>
    MonoFloat64,

    /// <summary>
    /// 64-bit floating point stereo buffer format.
    /// </summary>
    StereoFloat64,
}