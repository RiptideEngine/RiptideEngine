namespace RiptideMathematics;

partial struct Color {
    public static Color Black => new(0, 0, 0);
    public static Color Transparent => new(0, 0, 0, 0);
    public static Color Red => new(1, 0, 0);
    public static Color Green => new(0, 1, 0);
    public static Color Blue => new(0, 0, 1);
    public static Color White => new(1, 1, 1);
}