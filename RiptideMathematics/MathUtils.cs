namespace RiptideMathematics;

public static class MathUtils {
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T Remap<T>(T input, T inMin, T inMax, T outMin, T outMax) where T : IAdditionOperators<T, T, T>, ISubtractionOperators<T, T, T>, IMultiplyOperators<T, T, T>, IDivisionOperators<T, T, T> {
        return outMin + (input - inMin) * (outMax - outMin) / (inMax - inMin);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector2 Remap(Vector2 input, Vector2 inMin, Vector2 inMax, Vector2 outMin, Vector2 outMax) {
        return outMin + (input - inMin) * (outMax - outMin) / (inMax - inMin);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector3 Remap(Vector3 input, Vector3 inMin, Vector3 inMax, Vector3 outMin, Vector3 outMax) {
        return outMin + (input - inMin) * (outMax - outMin) / (inMax - inMin);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector4 Remap(Vector4 input, Vector4 inMin, Vector4 inMax, Vector4 outMin, Vector4 outMax) {
        return outMin + (input - inMin) * (outMax - outMin) / (inMax - inMin);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T AlignUpwardPow2<T>(T value, T alignment) where T : IBinaryInteger<T> => value + alignment - T.One & ~(alignment - T.One);
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T AlignDownwardPow2<T>(T value, T alignment) where T : IBinaryInteger<T> => value & ~(alignment - T.One);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Matrix4x4 CreateModel(Vector3 position, Quaternion rotation, Vector3 scale) {
        return Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool IsApproximate<T>(T a, T b, T threshold) where T : IFloatingPoint<T> {
        return T.Abs(a - b) <= threshold;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float GetSignedArea(Vector2 a, Vector2 b, Vector2 c) {
        return 0.5f * (a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool IsCollinear(Vector2 a, Vector2 b, Vector2 c, float threshold = 0.0001f) {
        var left = (a.Y - b.Y) * (a.X - c.X);
        var right = (a.Y - c.Y) * (a.X - b.X);

        return float.Abs(left - right) <= threshold;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool IsCollinear(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float threshold = 0.0001f) {
        return IsCollinear(a, b, c, threshold) && IsCollinear(b, c, d, threshold);
    }
}