namespace RiptideMathematics;

public sealed class Intersection {
    public static Vector2? Test(Ray2D ray1, Ray2D ray2) {
        var det = ray2.Direction.X * ray1.Direction.Y - ray2.Direction.Y * ray1.Direction.X;
        if (float.Abs(det) < float.Epsilon) return null;

        var dt = ray2.Position - ray1.Position;
        float u = (dt.Y * ray2.Direction.X - dt.X * ray2.Direction.Y) / det;
        
        return ray1.Position + ray1.Direction * u;
    }
}