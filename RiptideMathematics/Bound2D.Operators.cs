namespace RiptideMathematics;

partial struct Bound2D<T> {
    public static implicit operator Rectangle<T>(Bound2D<T> bound) => new(bound.MinX, bound.MinY, bound.MaxX - bound.MinX, bound.MaxY - bound.MinY);

    public static bool operator==(Bound2D<T> left, Bound2D<T> right) => left.Equals(right);
    public static bool operator!=(Bound2D<T> left, Bound2D<T> right) => !left.Equals(right);
}