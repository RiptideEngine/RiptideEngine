namespace RiptideMathematics;

partial class Bound2D {
    public static Bound2D<int> ToInt32(this Bound2D<float> bound) {
        if (Vector128.IsHardwareAccelerated) {
            var result = Vector128.ConvertToInt32(Vector128.LoadUnsafe(ref bound.MinX));
            return Unsafe.As<Vector128<int>, Bound2D<int>>(ref result);
        }

        return new((int)bound.MinX, (int)bound.MinY, (int)bound.MaxX, (int)bound.MaxY);
    }
}