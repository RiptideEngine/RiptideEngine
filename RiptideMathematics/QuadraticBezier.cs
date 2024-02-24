// ReSharper disable once UnusedParameter.Global

namespace RiptideMathematics;

// TODO: Boundary calculation.

public static class QuadraticBezier {
    public static Vector2 GetPosition(Vector2 start, Vector2 control, Vector2 end, float t) {
        return Vector2.Lerp(Vector2.Lerp(start, control, t), Vector2.Lerp(control, end, t), t);
    }
    
    public static Vector2 GetVelocity(Vector2 start, Vector2 control, Vector2 end, float t) {
        return 2 * ((1 - t) * (control - start) + t * (end - control));
    }
    
    public static Vector2 GetAcceleration(Vector2 start, Vector2 control, Vector2 end, float t) {
        return 2 * (end - 2 * control + start);
    }
}