namespace RiptideMathematics;

public static unsafe class FourierTransform {
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static int ReverseBits(int value, int bits) {
        int r = value;
        int count = bits - 1;

        value >>= 1;
        while (value > 0) {
            r = r << 1 | value & 1;
            count--;
            value >>= 1;
        }

        return r << count & (1 << bits) - 1;
    }

    public static void FFT(ReadOnlySpan<float> inputs, Span<Vector2> outputs) {
        if (inputs.IsEmpty || outputs.IsEmpty) return;
        if (inputs.Length != outputs.Length) throw new ArgumentException("Length of input span and length of output span mismatch.");
        if (!BitOperations.IsPow2(inputs.Length)) throw new ArgumentOutOfRangeException(nameof(inputs), "The length of input span isn't power of 2.");

        int N = inputs.Length;
        for (int i = 0; i < N; i++) {
            outputs[i] = new(inputs[i], 0);
        }

        FFT_Impl(outputs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void FFT(Span<Vector2> buffer) {
        if (buffer.IsEmpty) return;
        if (!BitOperations.IsPow2(buffer.Length)) throw new ArgumentException(nameof(buffer), "The length of input span isn't power of 2.");

        FFT_Impl(buffer);
    }

    private static void FFT_Impl(Span<Vector2> buffer) {
        // Implementation from https://rosettacode.org/wiki/Fast_Fourier_transform#C.23
        int bits = BitOperations.Log2((uint)buffer.Length);

        for (int j = 1; j < buffer.Length; j++) {
            int swapPos = ReverseBits(j, bits);
            if (swapPos <= j) continue;

            (buffer[j], buffer[swapPos]) = (buffer[swapPos], buffer[j]);
        }

        int len = buffer.Length;

        for (int N = 2; N <= len; N <<= 1) {
            for (int i = 0; i < len; i += N) {
                for (int k = 0, ke = N >> 1; k < ke; k++) {
                    int evenIndex = i + k;
                    int oddIndex = evenIndex + ke;
                    var even = buffer[evenIndex];
                    var odd = buffer[oddIndex];

                    (float sin, float cos) = MathF.SinCos(-2 * MathF.PI * k / N);

                    var exp = new Vector2(cos * odd.X - sin * odd.Y, sin * odd.X + cos * odd.Y);

                    buffer[evenIndex] = even + exp;
                    buffer[oddIndex] = even - exp;
                }
            }
        }
    }
}