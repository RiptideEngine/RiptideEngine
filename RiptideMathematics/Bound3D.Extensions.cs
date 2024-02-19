namespace RiptideMathematics;

partial struct Bound3D {
    public static bool TryGetIntersect(Bound3D left, Bound3D right, out Bound3D intersect) {
        Vector3 min = Vector3.Max(left.Min, right.Min);
        Vector3 max = Vector3.Min(left.Max, right.Max);

        if (min.X < max.X && min.Y < max.Y) {
            intersect = new(min, max - min);
            return true;
        }

        intersect = default;
        return false;
    }

    public static bool IsIntersect(Bound3D left, Bound3D right) {
        Vector3 min = Vector3.Max(left.Min, right.Min);
        Vector3 max = Vector3.Min(left.Max, right.Max);

        return min.X < max.X && min.Y < max.Y;
    }

    public static Bound3D GetIntersect(Bound3D left, Bound3D right) => TryGetIntersect(left, right, out var intersect) ? intersect : default;

    public static Bound3D Lerp(Bound3D a, Bound3D b, float weight) {
        return new(Vector3.Lerp(a.Min, b.Min, weight), Vector3.Lerp(a.Max, b.Max, weight));
    }

    public static Bound3D GetBoundingBox(Bound3D box, Matrix4x4 matrix) {
        var a = Vector3.Transform(box.Min, matrix);
        var b = Vector3.Transform(box.Min with { X = box.Max.X }, matrix);
        var c = Vector3.Transform(box.Min with { Y = box.Max.Y }, matrix);
        var d = Vector3.Transform(box.Min with { Z = box.Max.Z }, matrix);
        var e = Vector3.Transform(box.Max, matrix);
        var f = Vector3.Transform(box.Max with { X = box.Min.X }, matrix);
        var g = Vector3.Transform(box.Max with { Y = box.Min.Y }, matrix);
        var h = Vector3.Transform(box.Max with { Z = box.Min.Z }, matrix);
        
        var min = Vector3.Min(a, Vector3.Min(b, Vector3.Min(c, Vector3.Min(d, Vector3.Min(e, Vector3.Min(f, Vector3.Min(g, h)))))));
        var max = Vector3.Max(a, Vector3.Max(b, Vector3.Max(c, Vector3.Max(d, Vector3.Max(e, Vector3.Max(f, Vector3.Max(g, h)))))));

        return new(min, max);
    }
}