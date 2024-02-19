using RiptideMathematics;

namespace RiptideEngine.Audio;

public static class SampleConvert {
    public static short ToInt16(byte input) => (short)(input - 128 << 8 | input);
    public static float ToSingle(byte input) => input * 0.007843137254902f - 1f;

    public static byte ToByte(short value) => (byte)((value >> 8) + 128);
    public static float ToSingle(short value) => value * 0.000030517578125f;

    public static byte ToByte(float value) => (byte)(value * 127f + 128f);
    public static short ToInt16(float value) => (short)(value * 32767f);
}