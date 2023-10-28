namespace RiptideMathematics;

partial struct Color {
    public static Color Add(Color left, Color right) => left + right;
    public static Color Subtract(Color left, Color right) => left - right;
    public static Color Negate(Color color) => -color;
    public static Color Multiply(Color left, Color right) => left * right;
    public static Color Multiply(Color left, float scalar) => left * scalar;
    public static Color Divide(Color left, Color right) => left / right;
    public static Color Divide(Color left, float scalar) => left / scalar;

    public static Color operator+(Color left, Color right) {
        if (Vector128.IsHardwareAccelerated) {
            Vector128<float> result = Vector128.LoadUnsafe(ref left.R) + Vector128.LoadUnsafe(ref right.R);
            return Unsafe.As<Vector128<float>, Color>(ref result);
        }

        return new(left.R + right.R, left.G + right.G, left.B + right.B, left.A + right.A);
    }

    public static Color operator -(Color left, Color right) {
        if (Vector128.IsHardwareAccelerated) {
            Vector128<float> result = Vector128.LoadUnsafe(ref left.R) - Vector128.LoadUnsafe(ref right.R);
            return Unsafe.As<Vector128<float>, Color>(ref result);
        }

        return new(left.R - right.R, left.G - right.G, left.B - right.B, left.A - right.A);
    }

    public static Color operator -(Color color) {
        if (Vector128.IsHardwareAccelerated) {
            Vector128<float> result = -Vector128.LoadUnsafe(ref color.R);
            return Unsafe.As<Vector128<float>, Color>(ref result);
        }

        return new(-color.R, -color.G, -color.B, -color.A);
    }

    public static Color operator *(Color left, Color right) {
        if (Vector128.IsHardwareAccelerated) {
            Vector128<float> result = Vector128.LoadUnsafe(ref left.R) * Vector128.LoadUnsafe(ref right.R);
            return Unsafe.As<Vector128<float>, Color>(ref result);
        }

        return new(left.R * right.R, left.G * right.G, left.B * right.B, left.A * right.A);
    }

    public static Color operator *(Color left, float scalar) {
        if (Vector128.IsHardwareAccelerated) {
            Vector128<float> result = Vector128.LoadUnsafe(ref left.R) * Vector128.Create(scalar);
            return Unsafe.As<Vector128<float>, Color>(ref result);
        }

        return new(left.R * scalar, left.G * scalar, left.B * scalar, left.A * scalar);
    }

    public static Color operator /(Color left, Color right) {
        if (Vector128.IsHardwareAccelerated) {
            Vector128<float> result = Vector128.LoadUnsafe(ref left.R) / Vector128.LoadUnsafe(ref right.R);
            return Unsafe.As<Vector128<float>, Color>(ref result);
        }

        return new(left.R / right.R, left.G / right.G, left.B / right.B, left.A / right.A);
    }

    public static Color operator /(Color left, float scalar) {
        if (Vector128.IsHardwareAccelerated) {
            Vector128<float> result = Vector128.LoadUnsafe(ref left.R) / Vector128.Create(scalar);
            return Unsafe.As<Vector128<float>, Color>(ref result);
        }

        return new(left.R / scalar, left.G / scalar, left.B / scalar, left.A / scalar);
    }

    public static bool operator ==(Color left, Color right) => left.Equals(right);
    public static bool operator !=(Color left, Color right) => !(left == right);
}