namespace RiptideMathematics;

unsafe partial struct Vector3UInt {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt Add(Vector3UInt left, Vector3UInt right) {
        if (Sse2.IsSupported) {
            Vector128<uint> result = Sse2.Add(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector128<uint> result = AdvSimd.Add(AdvSimd.LoadVector128(&left.X), AdvSimd.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }

        return new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt Subtract(Vector3UInt left, Vector3UInt right) {
        if (Sse2.IsSupported) {
            Vector128<uint> result = Sse2.Subtract(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector128<uint> result = AdvSimd.Subtract(AdvSimd.LoadVector128(&left.X), AdvSimd.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }

        return new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt Multiply(Vector3UInt left, Vector3UInt right) {
        if (Sse41.IsSupported) {
            Vector128<uint> result = Sse41.MultiplyLow(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }
        if (Sse2.IsSupported) {
            var a = Sse2.LoadVector128(&left.X);
            var b = Sse2.LoadVector128(&right.X);

            Vector128<ulong> mul1 = Sse2.Multiply(a, b);
            Vector128<ulong> mul2 = Sse2.Multiply(Sse2.ShiftRightLogical128BitLane(a, 4), Sse2.ShiftRightLogical128BitLane(b, 4));

            Vector128<uint> result = Sse2.UnpackLow(Sse2.Shuffle(mul1.AsUInt32(), 0b00_00_10_00), Sse2.Shuffle(mul2.AsUInt32(), 0b00_00_10_00));

            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector128<uint> result = AdvSimd.Multiply(AdvSimd.LoadVector128(&left.X), AdvSimd.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }

        return new(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt Multiply(Vector3UInt left, uint right) => Multiply(left, new Vector3UInt(right));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt Multiply(uint left, Vector3UInt right) => Multiply(right, left);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Multiply(Vector3UInt left, float right) {
        if (AdvSimd.IsSupported) {
            Vector128<float> result = AdvSimd.Multiply(AdvSimd.ConvertToSingle(AdvSimd.LoadVector128(&left.X)), Vector128.Create(right));
            return Unsafe.As<Vector128<float>, Vector3>(ref result);
        }
        
        return new(left.X * right, left.Y * right, left.Z * right);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Multiply(float left, Vector3UInt right) => Multiply(right, left);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt Divide(Vector3UInt left, Vector3UInt right) {
        return new(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt Divide(Vector3UInt left, uint right) => new(left.X / right, left.Y / right, left.Z / right);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Divide(Vector3UInt left, float right) => new(left.X / right, left.Y / right, left.Z / right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt And(Vector3UInt left, Vector3UInt right) {
        if (Sse2.IsSupported) {
            Vector128<uint> result = Sse2.And(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector128<uint> result = AdvSimd.And(AdvSimd.LoadVector128(&left.X), AdvSimd.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }

        return new(left.X & right.X, left.Y & right.Y, left.Z & right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt Or(Vector3UInt left, Vector3UInt right) {
        if (Sse2.IsSupported) {
            Vector128<uint> result = Sse2.Or(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector128<uint> result = AdvSimd.Or(AdvSimd.LoadVector128(&left.X), AdvSimd.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }

        return new(left.X & right.X, left.Y & right.Y, left.Z & right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt Xor(Vector3UInt left, Vector3UInt right) {
        if (Sse2.IsSupported) {
            Vector128<uint> result = Sse2.Xor(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector128<uint> result = AdvSimd.Xor(AdvSimd.LoadVector128(&left.X), AdvSimd.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }

        return new(left.X & right.X, left.Y & right.Y, left.Z & right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt Complement(Vector3UInt value) {
        if (Sse2.IsSupported) {
            Vector128<uint> result = Sse2.Xor(Sse2.LoadVector128(&value.X), Vector128<uint>.AllBitsSet);
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector128<uint> result = AdvSimd.Not(AdvSimd.LoadVector128(&value.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }

        return new(~value.X, ~value.Y, ~value.Z);
    }

    // No bit-shifting because difference in C#'s behaviour and SSE2/AVX2 behaviours.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt Min(Vector3UInt left, Vector3UInt right) {
        if (Sse41.IsSupported) {
            Vector128<uint> result = Sse41.Min(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }

        // Could subtract both a and b and do signed comparison but could be slower since we did a lot of thing (compare, blending)
        //if (Sse2.IsSupported) {
        //    Vector128<uint> a = Sse2.LoadVector128(&left.X);
        //    Vector128<uint> b = Sse2.LoadVector128(&right.X);

        //    Vector128<uint> compare = Sse2.CompareLessThan(a, b);
        //    Vector128<uint> blend = Sse2.Or(Sse2.And(compare, a), Sse2.AndNot(compare, b));

        //    return Unsafe.As<Vector128<uint>, Vector3UInt>(ref blend);
        //}
        if (AdvSimd.IsSupported) {
            Vector128<uint> result = AdvSimd.Min(AdvSimd.LoadVector128(&left.X), AdvSimd.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }

        return new(uint.Min(left.X, right.X), uint.Min(left.Y, right.Y), uint.Min(left.Z, right.Z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt Max(Vector3UInt left, Vector3UInt right) {
        if (Sse41.IsSupported) {
            Vector128<uint> result = Sse41.Max(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }
        //if (Sse2.IsSupported) {
        //    Vector128<uint> a = Sse2.LoadVector128(&left.X);
        //    Vector128<uint> b = Sse2.LoadVector128(&right.X);

        //    Vector128<uint> compare = Sse2.CompareGreaterThan(a, b);
        //    Vector128<uint> blend = Sse2.Or(Sse2.And(compare, a), Sse2.AndNot(compare, b));

        //    return Unsafe.As<Vector128<uint>, Vector3UInt>(ref blend);
        //}
        if (AdvSimd.IsSupported) {
            Vector128<uint> result = AdvSimd.Max(AdvSimd.LoadVector128(&left.X), AdvSimd.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }

        return new(uint.Max(left.X, right.X), uint.Max(left.Y, right.Y), uint.Max(left.Z, right.Z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt Clamp(Vector3UInt value, Vector3UInt min, Vector3UInt max) {
        if (Sse41.IsSupported) {
            Vector128<uint> result = Sse41.Min(Sse41.Max(Sse2.LoadVector128(&value.X), Sse2.LoadVector128(&min.X)), AdvSimd.LoadVector128(&max.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }
        //if (Sse2.IsSupported) {
        //    return Min(Max(value, min), max);
        //}
        if (AdvSimd.IsSupported) {
            Vector128<uint> result = AdvSimd.Min(AdvSimd.Max(AdvSimd.LoadVector128(&value.X), AdvSimd.LoadVector128(&min.X)), AdvSimd.LoadVector128(&max.X));
            return Unsafe.As<Vector128<uint>, Vector3UInt>(ref result);
        }

        return new(uint.Min(uint.Max(value.X, min.X), max.X), uint.Min(uint.Max(value.Y, min.Y), max.Y), uint.Min(uint.Max(value.Z, min.Z), max.Z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Dot(Vector3UInt left, Vector3UInt right) {
        return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3UInt Cross(Vector3UInt vector1, Vector3UInt vector2) {
        return new(
            vector1.Y * vector2.Z - vector1.Z * vector2.Y,
            vector1.Z * vector2.X - vector1.X * vector2.Z,
            vector1.X * vector2.Y - vector1.Y * vector2.X
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint DistanceSquared(Vector3UInt a, Vector3UInt b) {
        var difference = Subtract(a, b);
        return Dot(difference, difference);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(Vector3UInt a, Vector3UInt b) => float.Sqrt(DistanceSquared(a, b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 SquareRoot(Vector3UInt vector) {
        return Vector3.SquareRoot(vector);
    }
}