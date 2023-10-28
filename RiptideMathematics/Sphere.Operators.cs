namespace RiptideMathematics;

partial struct Sphere<T> {
    public static bool operator ==(Sphere<T> left, Sphere<T> right) => left.Equals(right);
    public static bool operator !=(Sphere<T> left, Sphere<T> right) => !left.Equals(right);
}