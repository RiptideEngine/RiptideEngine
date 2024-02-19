namespace RiptideEngine.Core.Utils;

public static class VectorExtensions {
    public static void Deconstruct(this Vector2 vector, out float x, out float y) {
        x = vector.X;
        y = vector.Y;
    }

    public static void Deconstruct(this Vector3 vector, out Vector2 xy, out float z) {
        xy = Unsafe.As<Vector3, Vector2>(ref vector);
        z = vector.Z;
    }
    
    public static void Deconstruct(this Vector3 vector, out float x, out float y, out float z) {
        x = vector.X;
        y = vector.Y;
        z = vector.Z;
    }
    
    public static void Deconstruct(this Vector4 vector, out Vector2 xy, out float z, out float w) {
        xy = Unsafe.As<Vector4, Vector2>(ref vector);
        z = vector.Z;
        w = vector.W;
    }

    public static void Deconstruct(this Vector4 vector, out Vector3 xyz, out float w) {
        xyz = Unsafe.As<Vector4, Vector3>(ref vector);
        w = vector.W;
    }
    
    public static void Deconstruct(this Vector4 vector, out float x, out float y, out float z, out float w) {
        x = vector.X;
        y = vector.Y;
        z = vector.Z;
        w = vector.W;
    }
}