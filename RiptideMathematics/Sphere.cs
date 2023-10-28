namespace RiptideMathematics;

public static class Sphere {
    public static Vector3 GetPosition(this Sphere<float> sphere) => Unsafe.As<Sphere<float>, Vector3>(ref sphere);
}