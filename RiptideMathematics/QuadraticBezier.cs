// ReSharper disable once UnusedParameter.Global

namespace RiptideMathematics;

// TODO: Boundary calculation.

public static class QuadraticBezier {
    public static Vector2 GetPosition(Vector2 start, Vector2 control, Vector2 end, float t) {
        var weight = float.Clamp(t, 0, 1);
        var rweight = 1 - weight;

        return rweight * rweight * start + 2 * rweight * weight * control + weight * weight * end;
    }
    
    public static Vector2 GetVelocity(Vector2 start, Vector2 control, Vector2 end, float t) {
        var weight = float.Clamp(t, 0, 1);
        return 2 * (1 - weight) * (control - start) + 2 * weight * (end - control);
    }
}