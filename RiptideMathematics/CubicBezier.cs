namespace RiptideMathematics;

public static class CubicBezier {
    public static Vector2 GetPosition(Vector2 start, Vector2 startControl, Vector2 endControl, Vector2 end, float t) {
        var invT = 1 - t;

        return invT * invT * (invT * start + 3 * t * startControl) + t * t * (3 * invT * endControl + t * end);
    }
    
    public static Vector2 GetVelocity(Vector2 start, Vector2 startControl, Vector2 endControl, Vector2 end, float t) {
        var weight = float.Clamp(t, 0, 1);
        var rweight = 1 - weight;

        return 3 * (rweight * rweight * (startControl - start) + 2 * rweight * weight * (endControl - startControl) + weight * weight * (end - endControl));
    }

    public static Vector2 GetAcceleration(Vector2 start, Vector2 startControl, Vector2 endControl, Vector2 end, float t) {
        var weight = float.Clamp(t, 0, 1);
        return 6 * ((1 - weight) * (endControl - 2 * startControl + start) + weight * (end - 2 * endControl + startControl));
    }
}