﻿namespace RiptideMathematics;

unsafe partial struct Color {
    public static Color Saturate(Color color) {
        if (Vector128.IsHardwareAccelerated) {
            Vector128<float> result = Vector128.Max(Vector128.Min(Vector128.LoadUnsafe(ref color.R), Vector128<float>.One), Vector128<float>.Zero);
            return Unsafe.As<Vector128<float>, Color>(ref result);
        }

        return new(float.Clamp(color.R, 0, 1), float.Clamp(color.G, 0, 1), float.Clamp(color.B, 0, 1), float.Clamp(color.A, 0, 1));
    }

    public static Color Clamp(Color value, Color min, Color max) {
        if (Vector128.IsHardwareAccelerated) {
            Vector128<float> result = Vector128.Max(Vector128.Min(Vector128.LoadUnsafe(ref value.R), Vector128.LoadUnsafe(ref max.R)), Vector128<float>.Zero);
            return Unsafe.As<Vector128<float>, Color>(ref result);
        }

        return new(float.Clamp(value.R, min.R, max.R), float.Clamp(value.G, min.G, max.G), float.Clamp(value.B, min.B, max.B), float.Clamp(value.A, min.A, max.A));
    }

    public static Color Min(Color left, Color right) {
        if (Vector128.IsHardwareAccelerated) {
            Vector128<float> result = Vector128.Min(Vector128.LoadUnsafe(ref left.R), Vector128.LoadUnsafe(ref right.R));
            return Unsafe.As<Vector128<float>, Color>(ref result);
        }

        return new(float.Min(left.R, right.R), float.Min(left.G, right.G), float.Min(left.B, right.B), float.Min(left.A, right.A));
    }

    public static Color Max(Color left, Color right) {
        if (Vector128.IsHardwareAccelerated) {
            Vector128<float> result = Vector128.Max(Vector128.LoadUnsafe(ref left.R), Vector128.LoadUnsafe(ref right.R));
            return Unsafe.As<Vector128<float>, Color>(ref result);
        }

        return new(float.Max(left.R, right.R), float.Max(left.G, right.G), float.Max(left.B, right.B), float.Max(left.A, right.A));
    }

    public static Color Step(Color x, Color y) {
        if (Vector128.IsHardwareAccelerated) {
            Vector128<float> compare = Vector128.GreaterThanOrEqual(Vector128.LoadUnsafe(ref x.R), Vector128.LoadUnsafe(ref y.R));
            Vector128<float> result = Vector128.BitwiseAnd(Vector128<float>.One, compare);

            return Unsafe.As<Vector128<float>, Color>(ref result);
        }

        int* values = stackalloc int[4] {
            Unsafe.BitCast<bool, byte>(x.R >= y.R) * 0x3F800000,
            Unsafe.BitCast<bool, byte>(x.G >= y.G) * 0x3F800000,
            Unsafe.BitCast<bool, byte>(x.B >= y.B) * 0x3F800000,
            Unsafe.BitCast<bool, byte>(x.A >= y.A) * 0x3F800000,
        };

        return *(Color*)values;
    }

    public static Color OneMinus(Color color) {
        if (Vector128.IsHardwareAccelerated) {
            Vector128<float> result = Vector128<float>.One - Vector128.LoadUnsafe(ref color.R);
            return Unsafe.As<Vector128<float>, Color>(ref result);
        }

        return new(1 - color.R, 1 - color.G, 1 - color.B, 1 - color.A);
    }

    public static Color Lerp(Color a, Color b, float weight) {
        if (Vector128.IsHardwareAccelerated) {
            Vector128<float> w = Vector128.Create(weight);
            Vector128<float> result = Vector128.LoadUnsafe(ref a.R) * (Vector128<float>.One - w) + Vector128.LoadUnsafe(ref b.R) * w;
            return Unsafe.As<Vector128<float>, Color>(ref result);
        }

        return a * (1 - weight) + b * weight;
    }
}