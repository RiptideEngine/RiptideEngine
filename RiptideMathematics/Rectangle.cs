namespace RiptideMathematics;

public static unsafe class Rectangle {
    public static Rectangle<T> FromPosition<T>(T x, T y) where T : unmanaged, INumber<T> => new(x, y, T.Zero, T.Zero);
    public static Rectangle<T> FromSize<T>(T w, T h) where T : unmanaged, INumber<T> => new(T.Zero, T.Zero, w, h);

    public static Rectangle<float> Create(Vector2 position, Vector2 size) {
        Unsafe.SkipInit(out Rectangle<float> rect);

        Unsafe.Write(&rect.X, position);
        Unsafe.Write(&rect.W, size);

        return rect;
    }

    public static Vector2 GetPosition(this Rectangle<float> rect) => Unsafe.As<float, Vector2>(ref rect.X);
    public static Vector2 GetSize(this Rectangle<float> rect) => Unsafe.As<float, Vector2>(ref rect.W);
    public static Vector2 GetCenter(this Rectangle<float> rect) => Unsafe.As<float, Vector2>(ref rect.X) + Unsafe.As<float, Vector2>(ref rect.W) / 2;
    public static (T X, T Y) GetCenter<T>(this Rectangle<T> rect) where T : unmanaged, INumber<T> => (rect.X + rect.W / T.CreateChecked(2), rect.Y + rect.H / T.CreateChecked(2));

    public static bool TryGetIntersect<T>(Rectangle<T> left, Rectangle<T> right, out Rectangle<T> intersect) where T : unmanaged, INumber<T> {
        T leftX = T.Max(left.X, right.X);
        T rightX = T.Min(left.X + left.W, right.X + right.W);
        T bottomY = T.Max(left.Y, right.Y);
        T topY = T.Min(left.Y + left.H, right.Y + right.H);

        if (leftX < rightX && bottomY < topY) {
            intersect = new(leftX, bottomY, rightX - leftX, topY - bottomY);
            return true;
        }

        intersect = default;
        return false;
    }

    public static bool IsIntersect<T>(Rectangle<T> left, Rectangle<T> right) where T : unmanaged, INumber<T> {
        T leftX = T.Max(left.X, right.X);
        T rightX = T.Min(left.X + left.W, right.X + right.W);
        T bottomY = T.Max(left.Y, right.Y);
        T topY = T.Min(left.Y + left.H, right.Y + right.H);

        return leftX < rightX && bottomY < topY;
    }

    public static Rectangle<T> GetIntersect<T>(Rectangle<T> left, Rectangle<T> right) where T : unmanaged, INumber<T> => TryGetIntersect(left, right, out var intersect) ? intersect : default;

    public static T GetDistanceToNearestEdge<T>(Rectangle<T> rect, T x, T y) where T : unmanaged, INumber<T>, IRootFunctions<T> {
        T dx = T.Max(rect.X - x, T.Max(T.Zero, x - (rect.X + rect.W)));
        T dy = T.Max(rect.Y - y, T.Max(T.Zero, y - (rect.Y + rect.H)));

        return T.Hypot(dx, dy);
    }

    public static Rectangle<float> Scale(Rectangle<float> rect, Vector2 scale, Vector2 anchor) {
        var min = scale * (Unsafe.As<float, Vector2>(ref rect.X) - anchor) + anchor;
        var max = scale * (Unsafe.As<float, Vector2>(ref rect.X) + Unsafe.As<float, Vector2>(ref rect.W) - anchor) + anchor;

        return Create(min, max - min);
    }

    public static Rectangle<T> Scale<T>(Rectangle<T> rect, T scaleX, T scaleY, T anchorX, T anchorY) where T : unmanaged, INumber<T> {
        T minX = scaleX * (rect.X - anchorX) + anchorX, minY = scaleY * (rect.Y - anchorY) + anchorY;
        T maxX = scaleX * (rect.X + rect.W - anchorX) + anchorX, maxY = scaleY * (rect.Y + rect.H - anchorY) + anchorY;

        return new(minX, minY, maxX - minX, maxY - minY);
    }

    public static bool ContainsPoint(Rectangle<float> rect, Vector2 point) => ContainsPoint(rect, point.X, point.Y);
    public static bool ContainsPoint<T>(Rectangle<T> rect, T px, T py) where T : unmanaged, INumber<T> {
        return rect.X <= px && px <= rect.X + rect.W && rect.Y <= py && py <= rect.Y + rect.H;
    }

    public static Rectangle<float> InflateContain(Rectangle<float> rect, Vector2 point) {
        Vector2 min = Vector2.Min(Unsafe.As<float, Vector2>(ref rect.X), point);
        Vector2 max = Vector2.Max(Unsafe.As<float, Vector2>(ref rect.X) + Unsafe.As<float, Vector2>(ref rect.W), point);

        return Create(min, max - min);
    }
    public static Rectangle<T> InflateContain<T>(Rectangle<T> rect, T px, T py) where T : unmanaged, INumber<T> {
        T minX = T.Min(rect.X, px), minY = T.Min(rect.Y, py);
        T maxX = T.Max(rect.X + rect.W, px), maxY = T.Max(rect.Y + rect.H, py);

        return new(minX, minY, maxX - minX, maxY - minY);
    }
}