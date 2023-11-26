namespace RiptideMathematics;

partial struct Bound2DInt {
    public static bool TryGetIntersect(Bound2DInt left, Bound2DInt right, out Bound2DInt intersect) {
        Vector2Int min = Vector2Int.Max(left.Min, right.Min);
        Vector2Int max = Vector2Int.Min(left.Max, right.Max);

        if (min.X < max.X && min.Y < max.Y) {
            intersect = new(min, max - min);
            return true;
        }

        intersect = default;
        return false;
    }

    public static bool IsIntersect(Bound2DInt left, Bound2DInt right) {
        Vector2 min = Vector2.Max(left.Min, right.Min);
        Vector2 max = Vector2.Min(left.Max, right.Max);

        return min.X < max.X && min.Y < max.Y;
    }

    public static Bound2DInt GetIntersect(Bound2DInt left, Bound2DInt right) => TryGetIntersect(left, right, out var intersect) ? intersect : default;
}