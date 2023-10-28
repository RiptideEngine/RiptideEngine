namespace RiptideMathematics;

unsafe partial struct Rectangle<T> {
    // Call me insane but I implemented the & operator that returns the intersect part between 2 rectangles.

    public static implicit operator Bound2D<T>(Rectangle<T> rect) => new(rect.X, rect.Y, rect.X + rect.W, rect.Y + rect.H);

    public static bool operator==(Rectangle<T> left, Rectangle<T> right) => left.Equals(right);
    public static bool operator!=(Rectangle<T> left, Rectangle<T> right) => !left.Equals(right);
}