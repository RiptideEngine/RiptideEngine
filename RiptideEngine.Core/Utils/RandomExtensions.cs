namespace RiptideEngine.Core.Utils;

public static class RandomExtensions {
    public static Quaternion NextQuaternion(this Random random) {
        // https://stackoverflow.com/a/44031492

        float u = random.NextSingle();
        float v = random.NextSingle();
        float w = random.NextSingle();

        float sqrtU = float.Sqrt(u);
        float sqrtInvU = float.Sqrt(1 - u);
        (float sin2piV, float cos2piV) = float.SinCosPi(2 * v);
        (float sin2piW, float cos2piW) = float.SinCosPi(2 * w);

        return new Quaternion(sqrtInvU * sin2piV, sqrtInvU * cos2piV, sqrtU * sin2piW, sqrtU * cos2piW);
    }

    public static Quaternion NextQuaternionNonUniform(this Random random) {
        return Quaternion.Normalize(new Quaternion(random.NextSingle() - 0.5f, random.NextSingle() - 0.5f, random.NextSingle() - 0.5f, random.NextSingle() - 0.5f) * 2);
    }

    public static Vector2 NextVector2(this Random random) {
        return new(random.NextSingle(), random.NextSingle());
    }

    public static Vector3 NextVector3(this Random random) {
        return new(random.NextSingle(), random.NextSingle(), random.NextSingle());
    }

    public static Vector4 NextVector4(this Random random) {
        return new(random.NextSingle(), random.NextSingle(), random.NextSingle(), random.NextSingle());
    }
}