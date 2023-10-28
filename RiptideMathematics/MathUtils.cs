namespace RiptideMathematics;

public static class MathUtils {
    //public static Bound2D<T> GetRotatedRectangleBoundingBox<T>(Rectangle<T> rect, T angle) where T : unmanaged, INumber<T>, ITrigonometricFunctions<T> {
    //    (var sin, var cos) = T.SinCos(angle);

    //    var (centerX, centerY) = rect.GetCenter();

    //    T d0x = rect.X - centerX, d0y = rect.Y - centerY;
    //    T d1x = rect.X + rect.W - centerX, d1y = 
    //}

    public static T AlignUpwardPow2<T>(T value, T alignment) where T : IBinaryInteger<T> {
        return (value + alignment - T.One) & ~(alignment - T.One);
    }

    public static void ExtractYawPitchRoll(this Quaternion q, out float yaw, out float pitch, out float roll) {
        yaw = MathF.Atan2(2.0f * (q.Y * q.W + q.X * q.Z), 1.0f - 2.0f * (q.X * q.X + q.Y * q.Y));

        float sinp = 2.0f * (q.X * q.W - q.Y * q.Z);
        if (sinp >= 1) {
            pitch = float.Pi / 2 * float.Sign(sinp);
        } else {
            pitch = MathF.Asin(sinp);
        }

        roll = MathF.Atan2(2.0f * (q.X * q.Y + q.Z * q.W), 1.0f - 2.0f * (q.X * q.X + q.Z * q.Z));
    }

    public static float ExtractYaw(this Quaternion q) => MathF.Atan2(2.0f * (q.Y * q.W + q.X * q.Z), 1.0f - 2.0f * (q.X * q.X + q.Y * q.Y));
    public static float ExtractPitch(this Quaternion q) {
        float sinp = 2.0f * (q.X * q.W - q.Y * q.Z);
        return sinp >= 1 ? float.Pi / 2 * float.Sign(sinp) : MathF.Asin(sinp);
    }
    public static float ExtractRoll(this Quaternion q) => MathF.Atan2(2.0f * (q.X * q.Y + q.Z * q.W), 1.0f - 2.0f * (q.X * q.X + q.Z * q.Z));

    public static Matrix4x4 CreateModel(Vector3 position, Quaternion rotation, Vector3 scale) {
        return Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);
    }
}