namespace RiptideMathematics;

public sealed class Intersection {
    public static Vector2? Test(Ray2D ray1, Ray2D ray2) {
        var det = ray2.Direction.X * ray1.Direction.Y - ray2.Direction.Y * ray1.Direction.X;
        if (float.Abs(det) < float.Epsilon) return null;

        var dt = ray2.Position - ray1.Position;
        float u = (dt.Y * ray2.Direction.X - dt.X * ray2.Direction.Y) / det;
        float v = (dt.Y * ray1.Direction.X - dt.X * ray1.Direction.Y) / det;
        
        return u >= 0 && v >= 0 ? ray1.GetPosition(u) : null;
    }

    public static Vector2? Test(Line2D line1, Line2D line2) {
        var det = line2.Direction.X * line1.Direction.Y - line2.Direction.Y * line1.Direction.X;
        if (float.Abs(det) < float.Epsilon) return null;

        var dt = line2.Position - line1.Position;
        float u = (dt.Y * line2.Direction.X - dt.X * line2.Direction.Y) / det;
        
        return line1.GetPosition(u);
    }
}