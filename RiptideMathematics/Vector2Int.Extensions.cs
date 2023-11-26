namespace RiptideMathematics;

unsafe partial struct Vector2Int {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Add(Vector2Int left, Vector2Int right) {
        if (Sse2.IsSupported) {
            Vector128<int> result = Sse2.Add(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector2Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector64<int> result = AdvSimd.Add(AdvSimd.LoadVector64(&left.X), AdvSimd.LoadVector64(&right.X));
            return Unsafe.As<Vector64<int>, Vector2Int>(ref result);
        }

        return new(left.X + right.X, left.Y + right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Subtract(Vector2Int left, Vector2Int right) {
        if (Sse2.IsSupported) {
            Vector128<int> result = Sse2.Subtract(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector2Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector64<int> result = AdvSimd.Subtract(AdvSimd.LoadVector64(&left.X), AdvSimd.LoadVector64(&right.X));
            return Unsafe.As<Vector64<int>, Vector2Int>(ref result);
        }

        return new(left.X - right.X, left.Y - right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Negate(Vector2Int value) {
        if (Sse2.IsSupported) {
            Vector128<int> result = Sse2.Subtract(Vector128<int>.Zero, Sse2.LoadVector128(&value.X));
            return Unsafe.As<Vector128<int>, Vector2Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector64<int> result = AdvSimd.Negate(AdvSimd.LoadVector64(&value.X));
            return Unsafe.As<Vector64<int>, Vector2Int>(ref result);
        }

        return new(-value.X, -value.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Multiply(Vector2Int left, Vector2Int right) {
        if (Sse41.IsSupported) {
            Vector128<int> result = Sse41.MultiplyLow(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector2Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector64<int> result = AdvSimd.Multiply(AdvSimd.LoadVector64(&left.X), AdvSimd.LoadVector64(&right.X));
            return Unsafe.As<Vector64<int>, Vector2Int>(ref result);
        }

        return new(left.X * right.X, left.Y * right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Multiply(Vector2Int left, int right) => Multiply(left, new Vector2Int(right));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Multiply(int left, Vector2Int right) => Multiply(right, left);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Divide(Vector2Int left, Vector2Int right) {
        return new(left.X / right.X, left.Y / right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Divide(Vector2Int left, int right) => new(left.X / right, left.Y / right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Divide(Vector2Int left, float right) => new(left.X / right, left.Y / right);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int And(Vector2Int left, Vector2Int right) {
        if (Sse2.IsSupported) {
            Vector128<int> result = Sse2.And(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector2Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector64<int> result = AdvSimd.And(AdvSimd.LoadVector64(&left.X), AdvSimd.LoadVector64(&right.X));
            return Unsafe.As<Vector64<int>, Vector2Int>(ref result);
        }

        return new(left.X & right.X, left.Y & right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Or(Vector2Int left, Vector2Int right) {
        if (Sse2.IsSupported) {
            Vector128<int> result = Sse2.Or(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector2Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector64<int> result = AdvSimd.Or(AdvSimd.LoadVector64(&left.X), AdvSimd.LoadVector64(&right.X));
            return Unsafe.As<Vector64<int>, Vector2Int>(ref result);
        }

        return new(left.X & right.X, left.Y & right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Xor(Vector2Int left, Vector2Int right) {
        if (Sse2.IsSupported) {
            Vector128<int> result = Sse2.Xor(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector2Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector64<int> result = AdvSimd.Xor(AdvSimd.LoadVector64(&left.X), AdvSimd.LoadVector64(&right.X));
            return Unsafe.As<Vector64<int>, Vector2Int>(ref result);
        }

        return new(left.X & right.X, left.Y & right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Complement(Vector2Int value) {
        if (Sse2.IsSupported) {
            Vector128<int> result = Sse2.Xor(Sse2.LoadVector128(&value.X), Vector128<int>.AllBitsSet);
            return Unsafe.As<Vector128<int>, Vector2Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector64<int> result = AdvSimd.Not(AdvSimd.LoadVector64(&value.X));
            return Unsafe.As<Vector64<int>, Vector2Int>(ref result);
        }

        return new(~value.X, ~value.Y);
    }

    // No bit-shifting because difference in C#'s behaviour and SSE2/AVX2 behaviours.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Min(Vector2Int left, Vector2Int right) {
        if (Sse41.IsSupported) {
            Vector128<int> result = Sse41.Min(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector2Int>(ref result);
        }
        if (Sse2.IsSupported) {
            Vector128<int> a = Sse2.LoadVector128(&left.X);
            Vector128<int> b = Sse2.LoadVector128(&right.X);

            Vector128<int> compare = Sse2.CompareLessThan(a, b);
            Vector128<int> blend = Sse2.Or(Sse2.And(compare, a), Sse2.AndNot(compare, b));

            return Unsafe.As<Vector128<int>, Vector2Int>(ref blend);
        }
        if (AdvSimd.IsSupported) {
            Vector64<int> result = AdvSimd.Min(AdvSimd.LoadVector64(&left.X), AdvSimd.LoadVector64(&right.X));
            return Unsafe.As<Vector64<int>, Vector2Int>(ref result);
        }

        return new(int.Min(left.X, right.X), int.Min(left.Y, right.Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Max(Vector2Int left, Vector2Int right) {
        if (Sse41.IsSupported) {
            Vector128<int> result = Sse41.Max(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector2Int>(ref result);
        }
        if (Sse2.IsSupported) {
            Vector128<int> a = Sse2.LoadVector128(&left.X);
            Vector128<int> b = Sse2.LoadVector128(&right.X);

            Vector128<int> compare = Sse2.CompareGreaterThan(a, b);
            Vector128<int> blend = Sse2.Or(Sse2.And(compare, a), Sse2.AndNot(compare, b));

            return Unsafe.As<Vector128<int>, Vector2Int>(ref blend);
        }
        if (AdvSimd.IsSupported) {
            Vector64<int> result = AdvSimd.Max(AdvSimd.LoadVector64(&left.X), AdvSimd.LoadVector64(&right.X));
            return Unsafe.As<Vector64<int>, Vector2Int>(ref result);
        }

        return new(int.Max(left.X, right.X), int.Max(left.Y, right.Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Clamp(Vector2Int value, Vector2Int min, Vector2Int max) {
        if (Sse41.IsSupported) {
            Vector128<int> result = Sse41.Min(Sse41.Max(Sse2.LoadVector128(&value.X), Sse2.LoadVector128(&min.X)), AdvSimd.LoadVector128(&max.X));
            return Unsafe.As<Vector128<int>, Vector2Int>(ref result);
        }
        if (Sse2.IsSupported) {
            return Min(Max(value, min), max);
        }
        if (AdvSimd.IsSupported) {
            Vector64<int> result = AdvSimd.Min(AdvSimd.Max(AdvSimd.LoadVector64(&value.X), AdvSimd.LoadVector64(&min.X)), AdvSimd.LoadVector64(&max.X));
            return Unsafe.As<Vector64<int>, Vector2Int>(ref result);
        }

        return new(int.Min(int.Max(value.X, min.X), max.X), int.Min(int.Max(value.Y, min.Y), max.Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Dot(Vector2Int left, Vector2Int right) {
        return left.X * right.X + left.Y * right.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DistanceSquared(Vector2Int a, Vector2Int b) {
        var difference = Subtract(a, b);
        return Dot(difference, difference);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(Vector2Int a, Vector2Int b) => float.Sqrt(DistanceSquared(a, b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Abs(Vector2Int value) {
        if (Ssse3.IsSupported) {
            Vector128<uint> result = Ssse3.Abs(Sse2.LoadVector128(&value.X));
            return Unsafe.As<Vector128<uint>, Vector2Int>(ref result);
        }
        if (Sse2.IsSupported) {
            Vector128<int> result = Sse2.And(Sse2.LoadVector128(&value.X), Vector128.Create(0x7FFFFFFF));
            return Unsafe.As<Vector128<int>, Vector2Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector64<uint> result = AdvSimd.Abs(AdvSimd.LoadVector64(&value.X));
            return Unsafe.As<Vector64<uint>, Vector2Int>(ref result);
        }

        return new(value.X & 0x7FFFFFFF, value.Y & 0x7FFFFFFF);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Reflect(Vector2Int vector, Vector2Int normal) {
        var dot = Dot(vector, normal);
        return vector - (2 * dot * normal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 SquareRoot(Vector2Int vector) {
        return Vector2.SquareRoot(vector);
    }
}