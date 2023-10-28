namespace RiptideEngine.Core.Utils;

public static unsafe class UnsafeHelpers {
    public static nuint StringLength(byte* pointer) {
        if (pointer == null) return 0;

        nuint length = 0;

        if (Avx2.IsSupported) {
            while (((nuint)pointer) % (nuint)Vector256<byte>.Count != 0) {
                if (*pointer != 0) {
                    length++;
                    pointer++;
                } else return length;
            }

            while (true) {
                Vector256<byte> load = Avx.LoadAlignedVector256(pointer);
                Vector256<byte> compare = Avx2.CompareEqual(load, Vector256<byte>.Zero);
                var movemask = Vector256.ExtractMostSignificantBits(compare);

                if (movemask == 0) {
                    length += (nuint)Vector256<byte>.Count;
                    pointer += Vector256<byte>.Count;
                } else {
                    length += uint.TrailingZeroCount(movemask);
                    break;
                }
            }
        } else if (Sse2.IsSupported) {
            while (((nuint)pointer) % (nuint)Vector128<byte>.Count != 0) {
                if (*pointer != 0) {
                    length++;
                    pointer++;
                } else return length;
            }

            while (true) {
                Vector128<byte> load = Sse2.LoadAlignedVector128(pointer);
                Vector128<byte> compare = Sse2.CompareEqual(load, Vector128<byte>.Zero);
                var movemask = Vector128.ExtractMostSignificantBits(compare);

                if (movemask == 0) {
                    length += (nuint)Vector128<byte>.Count;
                    pointer += Vector128<byte>.Count;
                } else {
                    length += uint.TrailingZeroCount(movemask);
                    break;
                }
            }
        } else if (AdvSimd.IsSupported) {
            while (true) {
                Vector128<byte> load = AdvSimd.LoadVector128(pointer);
                Vector128<byte> compare = AdvSimd.CompareEqual(load, Vector128<byte>.Zero);
                var movemask = Vector128.ExtractMostSignificantBits(compare);

                if (movemask == 0) {
                    length += (nuint)Vector128<byte>.Count;
                    pointer += Vector128<byte>.Count;
                } else {
                    length += uint.TrailingZeroCount(movemask);
                    break;
                }
            }
        } else {
            while (*pointer != 0) {
                length++;
                pointer++;
            }
        }

        return length;
    }

    public static int StringCompare(byte* strA, byte* strB) {
        if (strA == null) return strB == null ? 0 : *strB;
        if (strB == null) return strA == null ? 0 : *strA;
        if (strA == strB) return 0;

        while (*strA != 0 && *strA == *strB) {
            strA++;
            strB++;
        }

        return *strA - *strB;
    }
}