namespace RiptideMathematics;

unsafe partial struct Vector3Int {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int Add(Vector3Int left, Vector3Int right) {
        if (Sse2.IsSupported) {
            Vector128<int> result = Sse2.Add(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector128<int> result = AdvSimd.Add(AdvSimd.LoadVector128(&left.X), AdvSimd.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }

        return new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int Subtract(Vector3Int left, Vector3Int right) {
        if (Sse2.IsSupported) {
            Vector128<int> result = Sse2.Subtract(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector128<int> result = AdvSimd.Subtract(AdvSimd.LoadVector128(&left.X), AdvSimd.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }

        return new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int Negate(Vector3Int value) {
        if (Sse2.IsSupported) {
            Vector128<int> result = Sse2.Subtract(Vector128<int>.Zero, Sse2.LoadVector128(&value.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector128<int> result = AdvSimd.Negate(AdvSimd.LoadVector128(&value.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }

        return new(-value.X, -value.Y, -value.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int Multiply(Vector3Int left, Vector3Int right) {
        if (Sse41.IsSupported) {
            Vector128<int> result = Sse41.MultiplyLow(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }
        if (Sse2.IsSupported) {
            var a = Sse2.LoadVector128(&left.X).AsUInt32();
            var b = Sse2.LoadVector128(&right.X).AsUInt32();

            Vector128<ulong> mul1 = Sse2.Multiply(a, b);
            Vector128<ulong> mul2 = Sse2.Multiply(Sse2.ShiftRightLogical128BitLane(a, 4), Sse2.ShiftRightLogical128BitLane(b, 4));

            Vector128<uint> result = Sse2.UnpackLow(Sse2.Shuffle(mul1.AsUInt32(), 0b00_00_10_00), Sse2.Shuffle(mul2.AsUInt32(), 0b00_00_10_00));

            return Unsafe.As<Vector128<uint>, Vector3Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector128<int> result = AdvSimd.Multiply(AdvSimd.LoadVector128(&left.X), AdvSimd.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }

        return new(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int Multiply(Vector3Int left, int right) => Multiply(left, new Vector3Int(right));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int Multiply(int left, Vector3Int right) => Multiply(right, left);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Multiply(Vector3Int left, float right) {
        if (Sse2.IsSupported) {
            Vector128<float> result = Sse.Multiply(Sse2.ConvertToVector128Single(Sse2.LoadVector128(&left.X)), Vector128.Create(right));
            return Unsafe.As<Vector128<float>, Vector3>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector128<float> result = AdvSimd.Multiply(AdvSimd.ConvertToSingle(AdvSimd.LoadVector128(&left.X)), Vector128.Create(right));
            return Unsafe.As<Vector128<float>, Vector3>(ref result);
        }
        
        return new(left.X * right, left.Y * right, left.Z * right);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Multiply(float left, Vector3Int right) => Multiply(right, left);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int Divide(Vector3Int left, Vector3Int right) {
        return new(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int Divide(Vector3Int left, int right) => new(left.X / right, left.Y / right, left.Z / right);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Divide(Vector3Int left, float right) => new(left.X / right, left.Y / right, left.Z / right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int And(Vector3Int left, Vector3Int right) {
        if (Sse2.IsSupported) {
            Vector128<int> result = Sse2.And(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector128<int> result = AdvSimd.And(AdvSimd.LoadVector128(&left.X), AdvSimd.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }

        return new(left.X & right.X, left.Y & right.Y, left.Z & right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int Or(Vector3Int left, Vector3Int right) {
        if (Sse2.IsSupported) {
            Vector128<int> result = Sse2.Or(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector128<int> result = AdvSimd.Or(AdvSimd.LoadVector128(&left.X), AdvSimd.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }

        return new(left.X & right.X, left.Y & right.Y, left.Z & right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int Xor(Vector3Int left, Vector3Int right) {
        if (Sse2.IsSupported) {
            Vector128<int> result = Sse2.Xor(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector128<int> result = AdvSimd.Xor(AdvSimd.LoadVector128(&left.X), AdvSimd.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }

        return new(left.X & right.X, left.Y & right.Y, left.Z & right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int Complement(Vector3Int value) {
        if (Sse2.IsSupported) {
            Vector128<int> result = Sse2.Xor(Sse2.LoadVector128(&value.X), Vector128<int>.AllBitsSet);
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector128<int> result = AdvSimd.Not(AdvSimd.LoadVector128(&value.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }

        return new(~value.X, ~value.Y, ~value.Z);
    }

    // No bit-shifting because difference in C#'s behaviour and SSE2/AVX2 behaviours.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int Min(Vector3Int left, Vector3Int right) {
        if (Sse41.IsSupported) {
            Vector128<int> result = Sse41.Min(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }
        if (Sse2.IsSupported) {
            Vector128<int> a = Sse2.LoadVector128(&left.X);
            Vector128<int> b = Sse2.LoadVector128(&right.X);

            Vector128<int> compare = Sse2.CompareLessThan(a, b);
            Vector128<int> blend = Sse2.Or(Sse2.And(compare, a), Sse2.AndNot(compare, b));

            return Unsafe.As<Vector128<int>, Vector3Int>(ref blend);
        }
        if (AdvSimd.IsSupported) {
            Vector128<int> result = AdvSimd.Min(AdvSimd.LoadVector128(&left.X), AdvSimd.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }

        return new(int.Min(left.X, right.X), int.Min(left.Y, right.Y), int.Min(left.Z, right.Z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int Max(Vector3Int left, Vector3Int right) {
        if (Sse41.IsSupported) {
            Vector128<int> result = Sse41.Max(Sse2.LoadVector128(&left.X), Sse2.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }
        if (Sse2.IsSupported) {
            Vector128<int> a = Sse2.LoadVector128(&left.X);
            Vector128<int> b = Sse2.LoadVector128(&right.X);

            Vector128<int> compare = Sse2.CompareGreaterThan(a, b);
            Vector128<int> blend = Sse2.Or(Sse2.And(compare, a), Sse2.AndNot(compare, b));

            return Unsafe.As<Vector128<int>, Vector3Int>(ref blend);
        }
        if (AdvSimd.IsSupported) {
            Vector128<int> result = AdvSimd.Max(AdvSimd.LoadVector128(&left.X), AdvSimd.LoadVector128(&right.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }

        return new(int.Max(left.X, right.X), int.Max(left.Y, right.Y), int.Max(left.Z, right.Z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int Clamp(Vector3Int value, Vector3Int min, Vector3Int max) {
        if (Sse41.IsSupported) {
            Vector128<int> result = Sse41.Min(Sse41.Max(Sse2.LoadVector128(&value.X), Sse2.LoadVector128(&min.X)), AdvSimd.LoadVector128(&max.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }
        if (Sse2.IsSupported) {
            return Min(Max(value, min), max);
        }
        if (AdvSimd.IsSupported) {
            Vector128<int> result = AdvSimd.Min(AdvSimd.Max(AdvSimd.LoadVector128(&value.X), AdvSimd.LoadVector128(&min.X)), AdvSimd.LoadVector128(&max.X));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }

        return new(int.Min(int.Max(value.X, min.X), max.X), int.Min(int.Max(value.Y, min.Y), max.Y), int.Min(int.Max(value.Z, min.Z), max.Z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Dot(Vector3Int left, Vector3Int right) {
        return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int Cross(Vector3Int vector1, Vector3Int vector2) {
        return new(
            vector1.Y * vector2.Z - vector1.Z * vector2.Y,
            vector1.Z * vector2.X - vector1.X * vector2.Z,
            vector1.X * vector2.Y - vector1.Y * vector2.X
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DistanceSquared(Vector3Int a, Vector3Int b) {
        var difference = Subtract(a, b);
        return Dot(difference, difference);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(Vector3Int a, Vector3Int b) => float.Sqrt(DistanceSquared(a, b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int Abs(Vector3Int value) {
        if (Ssse3.IsSupported) {
            Vector128<uint> result = Ssse3.Abs(Sse2.LoadVector128(&value.X));
            return Unsafe.As<Vector128<uint>, Vector3Int>(ref result);
        }
        if (Sse2.IsSupported) {
            Vector128<int> result = Sse2.And(Sse2.LoadVector128(&value.X), Vector128.Create(0x7FFFFFFF));
            return Unsafe.As<Vector128<int>, Vector3Int>(ref result);
        }
        if (AdvSimd.IsSupported) {
            Vector128<uint> result = AdvSimd.Abs(AdvSimd.LoadVector128(&value.X));
            return Unsafe.As<Vector128<uint>, Vector3Int>(ref result);
        }

        return new(value.X & 0x7FFFFFFF, value.Y & 0x7FFFFFFF, value.Z & 0x7FFFFFFF);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int Reflect(Vector3Int vector, Vector3Int normal) {
        var dot = Dot(vector, normal);
        return vector - (2 * dot * normal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 SquareRoot(Vector3Int vector) {
        return Vector3.SquareRoot(vector);
    }
}