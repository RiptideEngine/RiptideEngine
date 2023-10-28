namespace RiptideMathematics;

partial struct Bound3D<T> {
    public static bool operator ==(Bound3D<T> left, Bound3D<T> right) => left.Equals(right);
    public static bool operator !=(Bound3D<T> left, Bound3D<T> right) => !left.Equals(right);
}