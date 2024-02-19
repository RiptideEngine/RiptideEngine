namespace RiptideMathematics;

public static class CubicBezier {
    public static Vector2 GetPosition(Vector2 start, Vector2 startControl, Vector2 endControl, Vector2 end, float t) {
        var weight = float.Clamp(t, 0, 1);
        var rweight = 1 - weight;

        return rweight * rweight * (rweight * start + 3 * weight * startControl) + weight * weight * (3 * rweight * endControl + weight * end);
    }
    
    public static Vector2 GetVelocity(Vector2 start, Vector2 startControl, Vector2 endControl, Vector2 end, float t) {
        var weight = float.Clamp(t, 0, 1);
        var rweight = 1 - weight;

        return 3 * rweight * rweight * (startControl - start) + 6 * rweight * weight * (endControl - startControl) + 3 * weight * weight * (end - endControl);
    }

    public static Vector2 GetAcceleration(Vector2 start, Vector2 startControl, Vector2 endControl, Vector2 end, float t) {
        var weight = float.Clamp(t, 0, 1);
        return 6 * ((1 - weight) * (endControl - 2 * startControl + start) + weight * (end - 2 * endControl + startControl));
    }
}