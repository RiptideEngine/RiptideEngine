namespace RiptideMathematics;

public static class Bound3D {
    public static Vector3 GetMinimum(this Bound3D<float> bound) => Unsafe.As<float, Vector3>(ref bound.MinX);
    public static Vector3 GetMaximum(this Bound3D<float> bound) => Unsafe.As<float, Vector3>(ref bound.MaxX);

    public static Vector3 GetSize(this Bound3D<float> bound) => Unsafe.As<float, Vector3>(ref bound.MaxX) - Unsafe.As<float, Vector3>(ref bound.MinX);
}