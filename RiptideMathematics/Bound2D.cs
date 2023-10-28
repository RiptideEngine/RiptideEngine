namespace RiptideMathematics;

public static unsafe partial class Bound2D {
    public static Bound2D<float> Create(Vector2 min, Vector2 max) {
        Unsafe.SkipInit(out Bound2D<float> output);

        Unsafe.Write(&output.MinX, min);
        Unsafe.Write(&output.MaxX, max);

        return output;
    }

    public static Bound2D<float> Translate(Bound2D<float> bound, Vector2 delta) {
        return Create(Unsafe.As<float, Vector2>(ref bound.MinX) + delta, Unsafe.As<float, Vector2>(ref bound.MaxX) + delta);
    }
    public static Bound2D<T> Translate<T>(Bound2D<T> bound, T dx, T dy) where T : unmanaged, INumber<T> {
        if (Vector256.IsHardwareAccelerated) {
            if (typeof(T) == typeof(long)) {
                var result = Vector256.LoadUnsafe(ref Unsafe.As<T, long>(ref bound.MinX)) + Vector256.Create(Unsafe.As<T, long>(ref dx), Unsafe.As<T, long>(ref dy), Unsafe.As<T, long>(ref dx), Unsafe.As<T, long>(ref dy));
                return Unsafe.As<Vector256<long>, Bound2D<T>>(ref result);
            } else if (typeof(T) == typeof(ulong)) {
                var result = Vector256.LoadUnsafe(ref Unsafe.As<T, ulong>(ref bound.MinX)) + Vector256.Create(Unsafe.As<T, ulong>(ref dx), Unsafe.As<T, ulong>(ref dy), Unsafe.As<T, ulong>(ref dx), Unsafe.As<T, ulong>(ref dy));
                return Unsafe.As<Vector256<ulong>, Bound2D<T>>(ref result);
            }
        }

        if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported) {
            if (typeof(T) == typeof(byte)) {
                var result = Vector128.LoadUnsafe(ref Unsafe.As<T, byte>(ref bound.MinX)) + Vector128.Create(Unsafe.As<T, byte>(ref dx), Unsafe.As<T, byte>(ref dy), Unsafe.As<T, byte>(ref dx), Unsafe.As<T, byte>(ref dy), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                return Unsafe.As<Vector128<byte>, Bound2D<T>>(ref result);
            } else if (typeof(T) == typeof(sbyte)) {
                var result = Vector128.LoadUnsafe(ref Unsafe.As<T, sbyte>(ref bound.MinX)) + Vector128.Create(Unsafe.As<T, sbyte>(ref dx), Unsafe.As<T, sbyte>(ref dy), Unsafe.As<T, sbyte>(ref dx), Unsafe.As<T, sbyte>(ref dy), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                return Unsafe.As<Vector128<sbyte>, Bound2D<T>>(ref result);
            } else if (typeof(T) == typeof(short)) {
                var result = Vector128.LoadUnsafe(ref Unsafe.As<T, short>(ref bound.MinX)) + Vector128.Create(Unsafe.As<T, short>(ref dx), Unsafe.As<T, short>(ref dy), Unsafe.As<T, short>(ref dx), Unsafe.As<T, short>(ref dy), 0, 0, 0, 0);
                return Unsafe.As<Vector128<short>, Bound2D<T>>(ref result);
            } else if (typeof(T) == typeof(ushort)) {
                var result = Vector128.LoadUnsafe(ref Unsafe.As<T, ushort>(ref bound.MinX)) + Vector128.Create(Unsafe.As<T, ushort>(ref dx), Unsafe.As<T, ushort>(ref dy), Unsafe.As<T, ushort>(ref dx), Unsafe.As<T, ushort>(ref dy), 0, 0, 0, 0);
                return Unsafe.As<Vector128<ushort>, Bound2D<T>>(ref result);
            } else if (typeof(T) == typeof(int)) {
                var result = Vector128.LoadUnsafe(ref Unsafe.As<T, int>(ref bound.MinX)) + Vector128.Create(Unsafe.As<T, int>(ref dx), Unsafe.As<T, int>(ref dy), Unsafe.As<T, int>(ref dx), Unsafe.As<T, int>(ref dy));
                return Unsafe.As<Vector128<int>, Bound2D<T>>(ref result);
            } else if (typeof(T) == typeof(uint)) {
                var result = Vector128.LoadUnsafe(ref Unsafe.As<T, uint>(ref bound.MinX)) + Vector128.Create(Unsafe.As<T, uint>(ref dx), Unsafe.As<T, uint>(ref dy), Unsafe.As<T, uint>(ref dx), Unsafe.As<T, uint>(ref dy));
                return Unsafe.As<Vector128<uint>, Bound2D<T>>(ref result);
            } else if (typeof(T) == typeof(long)) {
                var adder = Vector128.Create(Unsafe.As<T, long>(ref dx), Unsafe.As<T, long>(ref dy));

                Vector128<long>* results = stackalloc Vector128<long>[2] {
                    Vector128.LoadUnsafe(ref Unsafe.As<T, long>(ref bound.MinX)) + adder,
                    Vector128.LoadUnsafe(ref Unsafe.As<T, long>(ref bound.MaxX)) + adder,
                };

                return *(Bound2D<T>*)results;
            } else if (typeof(T) == typeof(ulong)) {
                var adder = Vector128.Create(Unsafe.As<T, ulong>(ref dx), Unsafe.As<T, ulong>(ref dy));

                Vector128<ulong>* results = stackalloc Vector128<ulong>[2] {
                    Vector128.LoadUnsafe(ref Unsafe.As<T, ulong>(ref bound.MinX)) + adder,
                    Vector128.LoadUnsafe(ref Unsafe.As<T, ulong>(ref bound.MaxX)) + adder,
                };

                return *(Bound2D<T>*)results;
            }
        }

        return new(bound.MinX + dx, bound.MinY + dy, bound.MaxX + dx, bound.MaxY + dy);
    }

    public static Vector2 GetMinimum(this Bound2D<float> bound) => Unsafe.As<float, Vector2>(ref bound.MinX);
    public static Vector2 GetMaximum(this Bound2D<float> bound) => Unsafe.As<float, Vector2>(ref bound.MaxX);
}