namespace RiptideMathematics;

partial struct Rectangle2D {
    public static bool TryGetIntersect(Rectangle2D left, Rectangle2D right, out Rectangle2D intersect) {
        Vector2 min = Vector2.Max(left.Position, right.Position);
        Vector2 max = Vector2.Min(left.Position + left.Size, right.Position + right.Size);

        if (min.X < max.X && min.Y < max.Y) {
            intersect = new(min, max - min);
            return true;
        }

        intersect = default;
        return false;
    }

    public static bool IsIntersect(Rectangle2D left, Rectangle2D right) {
        Vector2 min = Vector2.Max(left.Position, right.Position);
        Vector2 max = Vector2.Min(left.Position + left.Size, right.Position + right.Size);

        return min.X < max.X && min.Y < max.Y;
    }

    public static Rectangle2D GetIntersect(Rectangle2D left, Rectangle2D right) => TryGetIntersect(left, right, out var intersect) ? intersect : default;

    public static float GetDistanceToNearestEdge(Rectangle2D rect, Vector2 point) {
        var v = Vector2.Max(rect.Position - point, Vector2.Max(Vector2.Zero, point - rect.Position - rect.Size));
        return float.Hypot(v.X, v.Y);
    }

    public static Rectangle2D Scale(Rectangle2D rect, Vector2 scale, Vector2 anchor) {
        var min = scale * (rect.Position - anchor) + anchor;
        var max = scale * (rect.Position + rect.Size - anchor) + anchor;

        return new(min, max - min);
    }

    public static bool ContainsPoint(Rectangle2D rect, Vector2 point) {
        var rectMax = rect.Position + rect.Size;

        return rect.Position.X <= point.X && point.X <= rectMax.X && rect.Position.Y <= point.Y && point.Y <= rectMax.Y;
    }

    //public static Rectangle2D InflateContain(Rectangle2D rect, Vector2 point) {
    //    Vector2 min = Vector2.Min(rect.Position, point);
    //    Vector2 max = Vector2.Max(rect.Position + rect.Size, point);

    //    return new(min, max - min);
    //}
}