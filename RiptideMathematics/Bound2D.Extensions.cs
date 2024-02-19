namespace RiptideMathematics;

partial struct Bound2D {
    public static bool TryGetIntersect(Bound2D left, Bound2D right, out Bound2D intersect) {
        Vector2 min = Vector2.Max(left.Min, right.Min);
        Vector2 max = Vector2.Min(left.Max, right.Max);

        if (min.X < max.X && min.Y < max.Y) {
            intersect = new(min, max - min);
            return true;
        }

        intersect = default;
        return false;
    }

    public static bool IsIntersect(Bound2D left, Bound2D right) {
        Vector2 min = Vector2.Max(left.Min, right.Min);
        Vector2 max = Vector2.Min(left.Max, right.Max);

        return min.X < max.X && min.Y < max.Y;
    }

    public static Bound2D GetIntersect(Bound2D left, Bound2D right) => TryGetIntersect(left, right, out var intersect) ? intersect : default;

    public static Bound2D Lerp(Bound2D a, Bound2D b, float weight) {
        return new(Vector2.Lerp(a.Min, b.Min, weight), Vector2.Lerp(a.Max, b.Max, weight));
    }
}