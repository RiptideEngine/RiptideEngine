namespace RiptideMathematics;

unsafe partial struct Vector2UInt {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt Add(Vector2UInt left, Vector2UInt right) {
        if (Sse2.IsSupported) {
            Vector128<uint> result = Sse2.Add(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector2UInt>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector64<uint> result = AdvSimd.Add(AdvSimd.LoadVector64(&left.X), AdvSimd.LoadVector64(&right.X));
            return Unsafe.As<Vector64<uint>, Vector2UInt>(ref result);
        }

        return new(left.X + right.X, left.Y + right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt Subtract(Vector2UInt left, Vector2UInt right) {
        if (Sse2.IsSupported) {
            Vector128<uint> result = Sse2.Subtract(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector2UInt>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector64<uint> result = AdvSimd.Subtract(AdvSimd.LoadVector64(&left.X), AdvSimd.LoadVector64(&right.X));
            return Unsafe.As<Vector64<uint>, Vector2UInt>(ref result);
        }

        return new(left.X - right.X, left.Y - right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt Multiply(Vector2UInt left, Vector2UInt right) {
        if (Sse41.IsSupported) {
            Vector128<uint> result = Sse41.MultiplyLow(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector2UInt>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector64<uint> result = AdvSimd.Multiply(AdvSimd.LoadVector64(&left.X), AdvSimd.LoadVector64(&right.X));
            return Unsafe.As<Vector64<uint>, Vector2UInt>(ref result);
        }

        return new(left.X * right.X, left.Y * right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt Multiply(Vector2UInt left, uint right) => Multiply(left, new Vector2UInt(right));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Multiply(Vector2UInt left, float right) {
        if (Sse2.IsSupported) {
            Vector128<uint> v = Sse2.LoadVector128(&left.X);
            
            Vector128<uint> msk_lo = Vector128.Create(0xFFFFu);
            Vector128<float> cnst65536f = Vector128.Create(65536.0f);

            Vector128<uint> v_lo = Sse2.And(v, msk_lo);
            Vector128<uint> v_hi = Sse2.ShiftRightLogical(v, 16);
            Vector128<float> v_lo_flt = Sse2.ConvertToVector128Single(v_lo.AsInt32());
            Vector128<float> v_hi_flt = Sse2.ConvertToVector128Single(v_hi.AsInt32());
            v_hi_flt  = Sse.Multiply(cnst65536f,v_hi_flt);

            Vector128<float> result = Sse.Multiply(Sse.Add(v_hi_flt, v_lo_flt), Vector128.Create(right));
    
            return Unsafe.As<Vector128<float>, Vector2>(ref result);
        }
        
        if (AdvSimd.IsSupported) {
            Vector64<float> result = AdvSimd.Multiply(AdvSimd.ConvertToSingle(AdvSimd.LoadVector64(&left.X)), Vector64.Create(right));
            return Unsafe.As<Vector64<float>, Vector2>(ref result);
        }
        
        return new(left.X * right, left.Y * right);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Multiply(float left, Vector2UInt right) => Multiply(right, left);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt Multiply(uint left, Vector2UInt right) => Multiply(right, left);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt Divide(Vector2UInt left, Vector2UInt right) {
        return new(left.X / right.X, left.Y / right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt Divide(Vector2UInt left, uint right) => new(left.X / right, left.Y / right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Divide(Vector2UInt left, float right) => new(left.X / right, left.Y / right);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt And(Vector2UInt left, Vector2UInt right) {
        if (Sse2.IsSupported) {
            Vector128<uint> result = Sse2.And(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector2UInt>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector64<uint> result = AdvSimd.And(AdvSimd.LoadVector64(&left.X), AdvSimd.LoadVector64(&right.X));
            return Unsafe.As<Vector64<uint>, Vector2UInt>(ref result);
        }

        return new(left.X & right.X, left.Y & right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt Or(Vector2UInt left, Vector2UInt right) {
        if (Sse2.IsSupported) {
            Vector128<uint> result = Sse2.Or(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector2UInt>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector64<uint> result = AdvSimd.Or(AdvSimd.LoadVector64(&left.X), AdvSimd.LoadVector64(&right.X));
            return Unsafe.As<Vector64<uint>, Vector2UInt>(ref result);
        }

        return new(left.X & right.X, left.Y & right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt Xor(Vector2UInt left, Vector2UInt right) {
        if (Sse2.IsSupported) {
            Vector128<uint> result = Sse2.Xor(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector2UInt>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector64<uint> result = AdvSimd.Xor(AdvSimd.LoadVector64(&left.X), AdvSimd.LoadVector64(&right.X));
            return Unsafe.As<Vector64<uint>, Vector2UInt>(ref result);
        }

        return new(left.X & right.X, left.Y & right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt Complement(Vector2UInt value) {
        if (Sse2.IsSupported) {
            Vector128<uint> result = Sse2.Xor(Sse2.LoadVector128(&value.X), Vector128<uint>.AllBitsSet);
            return Unsafe.As<Vector128<uint>, Vector2UInt>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector64<uint> result = AdvSimd.Not(AdvSimd.LoadVector64(&value.X));
            return Unsafe.As<Vector64<uint>, Vector2UInt>(ref result);
        }

        return new(~value.X, ~value.Y);
    }

    // No bit-shifting because difference in C#'s behaviour and SSE2/AVX2 behaviours.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt Min(Vector2UInt left, Vector2UInt right) {
        if (Sse41.IsSupported) {
            Vector128<uint> result = Sse41.Min(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector2UInt>(ref result);
        }
        if (Sse2.IsSupported) {
            Vector128<int> a = Sse2.LoadVector128(&left.X).AsInt32();
            Vector128<int> b = Sse2.LoadVector128(&right.X).AsInt32();

            Vector128<int> constants = Vector128.Create(0x80000000).AsInt32();
            
            Vector128<int> compare = Sse2.CompareLessThan(Sse2.Subtract(a, constants), Sse2.Subtract(b, constants));
            Vector128<int> blend = Sse2.Or(Sse2.And(compare, a), Sse2.AndNot(compare, b));

            return Unsafe.As<Vector128<int>, Vector2UInt>(ref blend);
        }
        if (AdvSimd.IsSupported) {
            Vector64<uint> result = AdvSimd.Min(AdvSimd.LoadVector64(&left.X), AdvSimd.LoadVector64(&right.X));
            return Unsafe.As<Vector64<uint>, Vector2UInt>(ref result);
        }

        return new(uint.Min(left.X, right.X), uint.Min(left.Y, right.Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt Max(Vector2UInt left, Vector2UInt right) {
        if (Sse41.IsSupported) {
            Vector128<uint> result = Sse41.Max(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<uint>, Vector2UInt>(ref result);
        }
        if (Sse2.IsSupported) {
            Vector128<int> a = Sse2.LoadVector128(&left.X).AsInt32();
            Vector128<int> b = Sse2.LoadVector128(&right.X).AsInt32();

            Vector128<int> constants = Vector128.Create(0x80000000).AsInt32();

            Vector128<int> compare = Sse2.CompareGreaterThan(Sse2.Subtract(a, constants), Sse2.Subtract(b, constants));
            Vector128<int> blend = Sse2.Or(Sse2.And(compare, a), Sse2.AndNot(compare, b));

            return Unsafe.As<Vector128<int>, Vector2UInt>(ref blend);
        }
        if (AdvSimd.IsSupported) {
            Vector64<uint> result = AdvSimd.Max(AdvSimd.LoadVector64(&left.X), AdvSimd.LoadVector64(&right.X));
            return Unsafe.As<Vector64<uint>, Vector2UInt>(ref result);
        }

        return new(uint.Max(left.X, right.X), uint.Max(left.Y, right.Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt Clamp(Vector2UInt value, Vector2UInt min, Vector2UInt max) {
        if (Sse41.IsSupported) {
            Vector128<uint> result = Sse41.Min(Sse41.Max(Sse2.LoadVector128(&value.X), Sse2.LoadVector128(&min.X)), Sse2.LoadVector128(&max.X));
            return Unsafe.As<Vector128<uint>, Vector2UInt>(ref result);
        }
        if (Sse2.IsSupported) {
            return Min(Max(value, min), max);
        }
        if (AdvSimd.IsSupported) {
            Vector64<uint> result = AdvSimd.Min(AdvSimd.Max(AdvSimd.LoadVector64(&value.X), AdvSimd.LoadVector64(&min.X)), AdvSimd.LoadVector64(&max.X));
            return Unsafe.As<Vector64<uint>, Vector2UInt>(ref result);
        }

        return new(uint.Clamp(value.X, min.X, max.Y), uint.Clamp(value.Y, min.Y, max.Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Dot(Vector2UInt left, Vector2UInt right) {
        return left.X * right.X + left.Y * right.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint DistanceSquared(Vector2UInt a, Vector2UInt b) {
        var difference = Subtract(a, b);
        return Dot(difference, difference);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(Vector2UInt a, Vector2UInt b) => float.Sqrt(DistanceSquared(a, b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2UInt Reflect(Vector2UInt vector, Vector2UInt normal) {
        var dot = Dot(vector, normal);
        return vector - 2 * dot * normal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 SquareRoot(Vector2UInt vector) {
        return Vector2.SquareRoot(vector);
    }
}